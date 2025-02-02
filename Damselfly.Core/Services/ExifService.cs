﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using System.Threading.Tasks;
using System.Threading;
using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to add/remove/update tags on image files on disk. This uses
    /// ExifTool to do the actual EXIF tag manipulation on the disk files,
    /// since there aren't currently any native C# libraries which can
    /// efficiently write tags to images without re-encoding the JPEG data
    /// (which would be lossy, and therefore destructive). Plus ExifTool
    /// is FAST!
    /// </summary>
    public class ExifService : IProcessJobFactory
    {
        public static string ExifToolVer { get; private set; }
        private readonly StatusService _statusService;
        private readonly ImageCache _imageCache;
        private readonly IndexingService _indexingService;
        private readonly WorkService _workService;

        public List<Tag> FavouriteTags { get; private set; } = new List<Tag>();
        public event Action OnFavouritesChanged;
        public event Action<List<string>> OnUserTagsAdded;
        private const int s_exifWriteDelay = 15;

        private void NotifyFavouritesChanged()
        {
            OnFavouritesChanged?.Invoke();
        }

        private void NotifyUserTagsAdded( List<string> tagsAdded )
        {
            OnUserTagsAdded?.Invoke(tagsAdded);
        }

        public ExifService( StatusService statusService, WorkService  workService,
                IndexingService indexingService, ImageCache imageCache )
        {
            _statusService = statusService;
            _imageCache = imageCache;
            _indexingService = indexingService;
            _workService = workService;

            GetExifToolVersion();
            LoadFavouriteTagsAsync().Wait();

            _workService.AddJobSource(this);
        }

        /// <summary>
        /// Check the ExifTool at startup
        /// </summary>
        private void GetExifToolVersion()
        {
            var process = new ProcessStarter();
            if (process.StartProcess("exiftool", "-ver") )
            {
                ExifToolVer = $"v{process.OutputText}";
            }

            if (string.IsNullOrEmpty(ExifToolVer))
            {
                ExifToolVer = "Unavailable - ExifTool Not found";
            }

            Logging.Log($"ExifVersion: {ExifToolVer}");
        }

        /// <summary>
        /// Add a list of IPTC tags to an image on the disk.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="tagToAdd"></param>
        /// <returns></returns>
        public async Task AddTagAsync(Image[] images, string tagToAdd)
        {
            var tagList = new List<string> { tagToAdd };
            await UpdateTagsAsync(images, tagList, null);
        }

        /// <summary>
        /// Takes an image and a set of keywords, and writes them to the DB queue for
        /// keywords to be added. These will then be processed asynchronously.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="tagsToAdd"></param>
        /// <param name="tagsToRemove"></param>
        /// <returns></returns>
        public async Task UpdateTagsAsync(Image image, List<string> addTags, List<string> removeTags = null, AppIdentityUser user = null)
        {
            await UpdateTagsAsync(new[] { image }, addTags, removeTags, user);
        }

      

        /// <summary>
        /// Takes an image and a set of keywords, and writes them to the DB queue for
        /// keywords to be added. These will then be processed asynchronously.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="tagsToAdd"></param>
        /// <param name="tagsToRemove"></param>
        /// <returns></returns>
        public async Task UpdateTagsAsync(Image[] images, List<string> addTags, List<string> removeTags = null, AppIdentityUser user = null )
        {
            // TODO: Split tags with commas here?
            var timestamp = DateTime.UtcNow;
            var changeDesc = string.Empty;

            using var db = new ImageContext();
            var keywordOps = new List<ExifOperation>();

            if (addTags != null)
            {
                var tagsToAdd = addTags.Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();

                foreach (var image in images)
                {
                    keywordOps.AddRange(tagsToAdd.Select(keyword => new ExifOperation
                    {
                        ImageId = image.ImageId,
                        Text = keyword.Sanitise(),
                        Type = ExifOperation.ExifType.Keyword,
                        Operation = ExifOperation.OperationType.Add,
                        TimeStamp = timestamp,
                        UserId = user?.Id
                    }));
                }

                changeDesc += $"added: {string.Join(',', tagsToAdd)}";
            }

            if (removeTags != null)
            {
                var tagsToRemove = removeTags.Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();

                foreach (var image in images)
                {
                    keywordOps.AddRange( tagsToRemove.Select(keyword => 
                        new ExifOperation
                        {
                            ImageId = image.ImageId,
                            Text = keyword.Sanitise(),
                            Type = ExifOperation.ExifType.Keyword,
                            Operation = ExifOperation.OperationType.Remove,
                            TimeStamp = timestamp
                        } ) );
                }

                if (!string.IsNullOrEmpty(changeDesc))
                    changeDesc += ", ";
                changeDesc += $"removed: {string.Join(',', tagsToRemove)}";
            }

            Logging.LogVerbose($"Bulk inserting {keywordOps.Count()} keyword operations (for {images.Count()}) into queue. ");

            try
            {
                await db.BulkInsert(db.KeywordOperations, keywordOps);

                _statusService.StatusText = $"Saved tags ({changeDesc}) for {images.Count()} images.";
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception inserting keyword operations: {ex.Message}");
            }

            if( user != null )
                NotifyUserTagsAdded(addTags);

            // Trigger the work service to look for new jobs
            _workService.HandleNewJobs(this, s_exifWriteDelay);
        }

        /// <summary>
        /// Helper method to actually run ExifTool and update the tags on disk.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="tagsToAdd"></param>
        /// <param name="tagsToRemove"></param>
        /// <returns></returns>
        private async Task<bool> ProcessExifOperations(int imageId, List<ExifOperation> exifOperations )
        {
            bool success = false;

            var image = await _imageCache.GetCachedImage(imageId);

            Logging.LogVerbose("Updating tags for file {0}", image.FullPath);
            string args = string.Empty;
            List<ExifOperation> processedOps = new List<ExifOperation>();

            foreach (var op in exifOperations)
            {
                var operationText = op.Text.RemoveSmartQuotes();

                if ( String.IsNullOrEmpty( operationText ) )
                {
                    Logging.LogWarning($"Exif Operation with empty text: {op.Image.FileName}.");
                    continue;
                }

                if (op.Type == ExifOperation.ExifType.Keyword)
                {
                    // Weird but important: we *alwaya* add a -= for the keyword,
                    // whether we're removing or adding it. Removing is self-evident,
                    // but adding is less intuitive. The reason is to avoid duplicate
                    // keywords. So if we do
                    //      "-keywords-=Banana -keywords+=Banana",
                    // this will remove the tag and re-add it if it already exists
                    // (creating a no-op) but the remove will do nothing if it doesn't
                    // exist. Thus, we ensure we don't add keywords twice.
                    // See: https://stackoverflow.com/questions/67282388/adding-multiple-keywords-with-exiftool-but-only-if-theyre-not-already-present
                    args += $" -keywords-=\"{operationText}\" ";

                    if (op.Operation == ExifOperation.OperationType.Remove)
                    {
                        Logging.LogVerbose($" Removing keyword {operationText} from {op.Image.FileName}");
                        processedOps.Add(op);
                    }
                    else if (op.Operation == ExifOperation.OperationType.Add)
                    {
                        Logging.LogVerbose($" Adding keyword '{operationText}' to {op.Image.FileName}");
                        args += $" -keywords+=\"{operationText}\" ";
                        processedOps.Add(op);
                    }
                }
            }

            // Note: we could do this to preserve the last-mod-time:
            //   args += " -P -overwrite_original_in_place";
            // However, we rely on the last-mod-time changing to pick up
            // changes to keywords and to subsequently re-index images.
            args += " -overwrite_original ";
            // We enable the 'ignore minor warnings' flag which will allow
            // us to do things like write tags that are too long for the
            // IPTC specification.
            args += " -m ";
            args += " \"" + image.FullPath + "\"";

            var process = new ProcessStarter();

            // Fix perl local/env issues for exiftool
            var env = new Dictionary<string, string>();
            env["LANGUAGE"] = "en_US.UTF-8";
            env["LANG"] = "en_US.UTF-8";
            env["LC_ALL"] = "en_US.UTF-8";

            Stopwatch watch = new Stopwatch("RunExifTool");
            success = process.StartProcess("exiftool", args, env);
            watch.Stop();

            var db = new ImageContext();

            if (success)
            {
                processedOps.ForEach(x => x.State = ExifOperation.FileWriteState.Written);

                // Updating the timestamp on the image to newer than its metadata will
                // trigger its metadata and tags to be refreshed during the next scan
                await _indexingService.MarkImagesForScan(new [] { image });
            }
            else
            {
                processedOps.ForEach(x => x.State = ExifOperation.FileWriteState.Failed);
                Logging.LogError("ExifTool Tag update failed for image: {0}", image.FullPath);

                RestoreTempExifImage(image.FullPath);
            }

            // Now write the updates
            await db.BulkUpdate(db.KeywordOperations, processedOps);

            var totals = string.Join(",", exifOperations.GroupBy(x => x.State)
                               .Select(x => $"{x.Key}: {x.Count()}")
                               .ToList());

            _statusService.StatusText = $"Keywords written for {image.FileName}. {totals}";

            return success;
        }

        /// <summary>
        /// If ExifTool fails, sometimes it can leave the temp file in place, meaning our image
        /// will go missing. So try and put it back.
        /// See: https://stackoverflow.com/questions/65870251/make-exiftool-overwrite-original-is-transactional-and-atomic
        /// </summary>
        /// <param name="imagePath"></param>
        private void RestoreTempExifImage(string imagePath)
        {
            FileInfo path = new FileInfo(imagePath);
            var newExtension = path.Extension + "_exiftool_tmp";
            var tempPath = new FileInfo( Path.ChangeExtension(imagePath, newExtension) );

            if (!path.Exists && tempPath.Exists)
            {
                Logging.Log($"Moving {tempPath.FullName} to {path.Name}...");
                try
                {
                    File.Move(tempPath.FullName, path.FullName);
                }
                catch( Exception ex )
                {
                    Logging.LogWarning($"Unable to move file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clean up processed keyword operations
        /// </summary>
        /// <param name="cleanupFreq"></param>
        public async Task CleanUpKeywordOperations(TimeSpan cleanupFreq)
        {
            using var db = new ImageContext();

            // Clean up completed operations older than 24hrs
            var cutOff = DateTime.UtcNow.AddDays(-1);

            try
            {
                int cleanedUp = await db.BatchDelete(db.KeywordOperations.Where(op => op.State == ExifOperation.FileWriteState.Written
                                                                         && op.TimeStamp < cutOff));

                Logging.LogVerbose($"Cleaned up {cleanedUp} completed Keyword Operations.");
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception whilst cleaning up keyword operations: {ex.Message}");
            }
        }


        /// <summary>
        /// Takes a list of time-ordered keyword operations (add/remove) and for
        /// each keyword conflates down to a single distinct operation. So if there
        /// was:
        ///     Image1 Add 'cat'
        ///     Image2 Add 'cat'
        ///     Image2 Remove 'cat'
        ///     Image2 Add 'dog'
        ///     Image2 Add 'cat'
        ///     Image1 Remove 'cat'
        /// THen this would conflate down to: 
        ///     Image2 Add 'cat'
        ///     Image2 Add 'dog'
        ///     Image1 Remove 'cat'
        /// </summary>
        /// <param name="tagsToProcess"></param>
        /// <returns></returns>
        private async Task<IDictionary<int, List<ExifOperation>>> ConflateOperations(List<ExifOperation> opsToProcess )
        {
            // The result is the image ID, and a list of conflated ops.
            var result = new Dictionary<int, List<ExifOperation>>();
            var discardedOps = new List<ExifOperation>();

            // First, conflate the keywords.
            var imageKeywords = opsToProcess.Where( x => x.Type == ExifOperation.ExifType.Keyword )
                                            .GroupBy(x => x.Image.ImageId);

            foreach( var imageOpList in imageKeywords )
            {
                var theImage = imageOpList.Key;
                var keywordOps = imageOpList.GroupBy(x => x.Text );

                // Now, for each exif change for this image, collect up the most
                // recent operation and store it in a dictionary. Note, text in
                // operations are case-sensitive (so ops for tag 'Cat' do not
                // conflate with ops for tag 'cat'
                var exifOpDict = new Dictionary<string, ExifOperation>();

                foreach( var op in keywordOps )
                {
                    var orderedOps = op.OrderBy(x => x.TimeStamp).ToList();

                    foreach( var imageKeywordOp in orderedOps )
                    {
                        if( exifOpDict.TryGetValue( imageKeywordOp.Text, out var existing ) )
                        {
                            // Update the state before it's replaced in the dict.
                            discardedOps.Add( existing ); 
                        }

                        // Store the most recent op for each operation,
                        // over-writing the previous
                        exifOpDict[imageKeywordOp.Text] = imageKeywordOp;
                    }
                }

                // By now we've got a dictionary of keywords/operations. Now we just
                // add them into the final result.
                result[theImage] = exifOpDict.Values.ToList();
            }

            // Now the captions. Group by image + list of ops, sorted newest first, and then the
            // one we want is the most recent.
            var imageCaptions = opsToProcess.Where(x => x.Type == ExifOperation.ExifType.Caption)
                                            .GroupBy( x => x.Image )
                                            .Select(x => new { Image = x.Key, NewestFirst = x.OrderByDescending(d => d.TimeStamp) } )
                                            .Select( x => new {
                                                   Image = x.Image,
                                                   Newest = x.NewestFirst.Take(1).ToList(),
                                                   Discarded = x.NewestFirst.Skip(1).ToList() })
                                            .ToList();

            // Now collect up the caption updates, and mark the rest as discarded.
            foreach( var pair in imageCaptions )
            {
                // Add the most recent to the result
                result[pair.Image.ImageId] = pair.Newest;
                discardedOps.AddRange(pair.Discarded);
            }


            if (discardedOps.Any())
            {
                var db = new ImageContext();

                // Mark the ops as discarded, and save them.
                discardedOps.ForEach(x => x.State = ExifOperation.FileWriteState.Discarded);

                Logging.Log($"Discarding {discardedOps.Count} duplicate EXIF operations.");
                Stopwatch watch = new Stopwatch("WriteDiscardedExifOps");
                await db.BulkUpdate(db.KeywordOperations, discardedOps);
                watch.Stop();
            }

            return result;
        }

        /// <summary>
        /// Switches a tag from favourite, to not favourite, or back
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task ToggleFavourite( Tag tag )
        {
            using var db = new ImageContext();
            // TODO: Async - use BulkUpdateAsync?
            tag.Favourite = !tag.Favourite;

            db.Tags.Update(tag);
            await db.SaveChangesAsync("Tag favourite");

            await LoadFavouriteTagsAsync();
        }

        /// <summary>
        /// Loads the favourite tags from the DB.
        /// </summary>
        /// <returns></returns>
        private async Task LoadFavouriteTagsAsync()
        {
            using var db = new ImageContext();

            // TODO: Clear the tag cache and reload, and get this from the cache
            var faves = await Task.FromResult(db.Tags
                                        .Where(x => x.Favourite)
                                        .OrderBy(x => x.Keyword)
                                        .ToList());

            if (!faves.SequenceEqual(FavouriteTags))
            {
                FavouriteTags.Clear();
                FavouriteTags.AddRange(faves);

                NotifyFavouritesChanged();
            }
        }

        public class ExifProcess : IProcessJob
        {
            public int ImageId { get; set; }
            public List<ExifOperation> ExifOps { get; set; }
            public ExifService Service { get; set; }
            public bool CanProcess => true;
            public string Description => "Keyword Updates";
            public JobPriorities Priority => JobPriorities.ExifService;

            public async Task Process()
            {
                await Service.ProcessExifOperations(ImageId, ExifOps);
            }
        }

        public JobPriorities Priority => JobPriorities.ExifService;

        public async Task<ICollection<IProcessJob>> GetPendingJobs(int maxCount)
        {
            var db = new ImageContext();

            // We skip any operations where the timestamp is more recent than 30s
            var timeThreshold = DateTime.UtcNow.AddSeconds(-1 * s_exifWriteDelay);

            // Find all the operations that are pending, and the timestamp is older than the threshold.
            var opsToProcess = await db.KeywordOperations.AsQueryable()
                                    .Where(x => x.State == ExifOperation.FileWriteState.Pending && x.TimeStamp < timeThreshold)
                                    .OrderByDescending(x => x.TimeStamp)
                                    .Take(maxCount)
                                    .Include(x => x.Image)
                                    .ToListAsync();

            var conflatedOps = await ConflateOperations(opsToProcess);

            var jobs = conflatedOps.Select(x => new ExifProcess
            {
                ImageId = x.Key,
                ExifOps = x.Value,
                Service = this
            }).ToArray();

            return jobs;
        }

    }
}
