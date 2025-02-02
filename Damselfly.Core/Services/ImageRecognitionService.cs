﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Utils.Images;
using Damselfly.Core.Utils.ML;
using Damselfly.ML.Face.Accord;
using Damselfly.ML.Face.Azure;
using Damselfly.ML.Face.Emgu;
using Damselfly.ML.ObjectDetection;
using Damselfly.ML.ImageClassification;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Services
{
    public class ImageRecognitionService : IProcessJobFactory
    {
        private readonly ObjectDetector _objectDetector;
        private readonly AccordFaceService _accordFaceService;
        private readonly AzureFaceService _azureFaceService;
        private readonly EmguFaceService _emguFaceService;
        private readonly StatusService _statusService;
        private readonly MetaDataService _metdataService;
        private readonly ThumbnailService _thumbService;
        private readonly ConfigService _configService;
        private readonly ImageClassifier _imageClassifier;
        private readonly WorkService _workService;
        private readonly ImageCache _imageCache;

        private IDictionary<string, Person> _peopleCache;

        public static bool EnableImageRecognition { get; set; } = true;

        public ImageRecognitionService(StatusService statusService, ObjectDetector objectDetector,
                        MetaDataService metadataService, AzureFaceService azureFace,
                        AccordFaceService accordFace, EmguFaceService emguService,
                        ThumbnailService thumbs, ConfigService configService,
                        ImageClassifier imageClassifier, ImageCache imageCache,
                        WorkService workService)
        {
            _thumbService = thumbs;
            _accordFaceService = accordFace;
            _azureFaceService = azureFace;
            _statusService = statusService;
            _objectDetector = objectDetector;
            _metdataService = metadataService;
            _emguFaceService = emguService;
            _configService = configService;
            _imageClassifier = imageClassifier;
            _imageCache = imageCache;
            _workService = workService;
         }

        public ImageRecognitionService()
        {
        }

        public List<Person> GetCachedPeople()
        {
            return _peopleCache.Values.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Initialise the in-memory cache of people.
        /// </summary>
        /// <param name="force"></param>
        private void LoadPersonCache(bool force = false)
        {
            try
            {
                if (_peopleCache == null || force)
                {
                    var watch = new Stopwatch("LoadPersonCache");

                    using (var db = new ImageContext())
                    {
                        // Pre-cache tags from DB.
                        _peopleCache = new ConcurrentDictionary<string, Person>(db.People
                                                            .Where(x => !string.IsNullOrEmpty(x.AzurePersonId))
                                                            .AsNoTracking()
                                                            .ToDictionary(k => k.AzurePersonId, v => v));
                        if (_peopleCache.Any())
                            Logging.LogTrace("Pre-loaded cach with {0} people.", _peopleCache.Count());
                    }

                    watch.Stop();
                }
            }
            catch (Exception ex)
            {
                Logging.LogError($"Unexpected exception loading people cache: {ex.Message}");
            }
        }

        public async Task UpdateName( ImageObject faceObject, string name )
        {
            if (!faceObject.IsFace)
                throw new ArgumentException("Image object passed to name update.");

            using var db = new ImageContext();

            if (faceObject.Person == null)
            {
                faceObject.Person = new Person();
                db.People.Add(faceObject.Person);
            }
            else
                db.People.Update(faceObject.Person);

            // TODO: If this is an existing person/name, we might need to merge in Azure
            faceObject.Person.Name = name;
            faceObject.Person.State = Person.PersonState.Identified;
            db.ImageObjects.Update(faceObject);

            await db.SaveChangesAsync("SetName");

            // Add/update the cache
            _peopleCache[faceObject.Person.AzurePersonId] = faceObject.Person;

        }

        public async Task UpdateName( Person person, string name )
        {
            using var db = new ImageContext();

            // TODO: If this is an existing person/name, we might need to merge in Azure
            person.Name = name;
            person.State = Person.PersonState.Identified;
            db.People.Update(person);

            await db.SaveChangesAsync("SetName");

            // Add/update the cache
            _peopleCache[person.AzurePersonId] = person;
        }

        /// <summary>
        /// Create the DB entries for people who we don't know about,
        /// and then pre-populate the cache with their entries.
        /// </summary>
        /// <param name="personIdsToAdd"></param>
        /// <returns></returns>
        public async Task<List<Person>> CreateMissingPeople(IEnumerable<string> personIdsToAdd)
        {
            using ImageContext db = new ImageContext();

            // Find the people that aren't already in the cache and add new ones
            var newPeople = personIdsToAdd.Where(x => !_peopleCache.ContainsKey(x))
                        .Select(x => new Person
                        {
                            AzurePersonId = x,
                            Name = "Unknown",
                            State = Person.PersonState.Unknown
                        }
                        ).ToList();


            if (newPeople.Any())
            {

                Logging.LogTrace("Adding {0} people", newPeople.Count());

                await db.BulkInsert(db.People, newPeople);

                // Add the new items to the cache. 
                newPeople.ForEach(x => _peopleCache[x.AzurePersonId] = x);
            }

            var allTags = personIdsToAdd.Select(x => _peopleCache[x]).ToList();
            return allTags;
        }

        /// <summary>
        /// Given a collection of detected objects, create the tags, put them in the cache,
        /// and then return a list of keyword => TagID key-value pairs
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        private async Task<IDictionary<string, int>> CreateNewTags(IList<ImageDetectResult> objects)
        {
            var allLabels = objects.Select(x => x.Tag).Distinct().ToList();
            var tags = await _metdataService.CreateTagsFromStrings(allLabels);

            return tags.ToDictionary(x => x.Keyword, y => y.TagId, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if ImagesWithFaces is selected and any of the 
        /// objects contains either a person or a face
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        private bool UseAzureForRecogition(IList<ImageDetectResult> objects)
        {
            if (_azureFaceService.DetectionType == AzureFaceService.AzureDetection.ImagesWithFaces)
            {
                if (objects.Any(x => string.Compare(x.Tag, "face", true) == 0 ||
                                     string.Compare(x.Tag, "person", true) == 0))
                {
                    return true;
                }
            }

            return false;
        }

#pragma warning disable 1416
        private System.Drawing.Bitmap SafeLoadBitmap(string fileName)
        {
            System.Drawing.Bitmap bmp = null;
            try
            {
                // Load the bitmap once
                bmp = new System.Drawing.Bitmap(fileName);
            }
            catch( Exception ex )
            {
                Logging.LogError($"Error loading bitmap for {fileName}: {ex}");
            }

            return bmp;
        }
#pragma warning restore 1416

        /// <summary>
        /// Detect objects in the image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private async Task DetectObjects(ImageMetaData metadata)
        {
            var image = metadata.Image;
            var file = new FileInfo(image.FullPath);
            var thumbSize = ThumbSize.Large;
            var medThumb = new FileInfo(_thumbService.GetThumbPath(file, thumbSize));
            var fileName = Path.Combine(image.Folder.Path, image.FileName);

            // First, update the timestamp. We do this first, so that even if something
            // fails, it'll be set, avoiding infinite loops of failed processing.
            // The caller will update the DB with a SaveChanges call.
            metadata.AILastUpdated = DateTime.UtcNow;

            try
            {
                var foundObjects = new List<ImageObject>();
                var foundFaces = new List<ImageObject>();
                Logging.Log($"Processing AI image detection for {file.Name}...");

                if (!File.Exists(medThumb.FullName) )
                {
                    // The thumb isn't ready yet. 
                    return;
                }

                var bitmap = SafeLoadBitmap(medThumb.FullName);

                if (bitmap == null)
                    return;

                if( _imageClassifier != null )
                {
                    var colorWatch = new Stopwatch("DetectObjects");

                    var dominant = _imageClassifier.DetectDominantColour(bitmap);
                    var average = _imageClassifier.DetectAverageColor(bitmap);

                    colorWatch.Stop();

                    image.MetaData.AverageColor = average.ToHex();
                    image.MetaData.DominantColor = dominant.ToHex();

                    Logging.LogVerbose($"Image {image.FullPath} has dominant colour {dominant.ToHex()}, average {average.ToHex()}");
                }

                // Next, look for faces. We need to determine if we:
                //  a) Use only local (Accord.Net) detection
                //  b) Use local detection, and then if we find a face, or a person object, submit to Azure
                //  c) Always submit every image to Azure.
                // This is a user config.
                bool useAzureDetection = false;

                var objwatch = new Stopwatch("DetectObjects");

                // First, look for Objects
                var objects = await _objectDetector.DetectObjects(bitmap);

                objwatch.Stop();

                if (objects.Any())
                {
                    Logging.Log($" Yolo found {objects.Count} objects in {fileName}...");

                    var newTags = await CreateNewTags(objects);

                    var newObjects = objects.Select(x => new ImageObject
                    {
                        RecogntionSource = ImageObject.RecognitionType.MLNetObject,
                        ImageId = image.ImageId,
                        RectX = (int)x.Rect.Left,
                        RectY = (int)x.Rect.Top,
                        RectHeight = (int)x.Rect.Height,
                        RectWidth = (int)x.Rect.Width,
                        TagId = x.IsFace ? 0 : newTags[x.Tag],
                        Type = ImageObject.ObjectTypes.Object.ToString(),
                        Score = x.Score
                    }).ToList();

                    if (UseAzureForRecogition(objects))
                        useAzureDetection = true;

                    ScaleObjectRects(image, newObjects, bitmap);
                    foundObjects.AddRange(newObjects);
                }

                if (_azureFaceService.DetectionType == AzureFaceService.AzureDetection.AllImages)
                {
                    // Skip local face detection and just go straight to Azure
                    useAzureDetection = true;
                }
                else
                {
                    if (_emguFaceService.ServiceAvailable)
                    {
                        var emguwatch = new Stopwatch("EmguFaceDetect");

                        var rects = _emguFaceService.DetectFaces(medThumb.FullName);

                        emguwatch.Stop();

                        if (UseAzureForRecogition(rects))
                        {
                            // Filter out the faces if we're using Azure
                            rects = rects.Where(x => !x.IsFace).ToList();
                            useAzureDetection = true;
                        }

                        if (rects.Any())
                        {
                            // Azure is disabled, so just use what we've got.
                            Logging.Log($" Emgu found {rects.Count} faces in {fileName}...");

                            var newTags = await CreateNewTags(rects);

                            var newObjects = rects.Select(x => new ImageObject
                            {
                                RecogntionSource = ImageObject.RecognitionType.Emgu,
                                ImageId = image.ImageId,
                                RectX = x.Rect.Left,
                                RectY = x.Rect.Top,
                                RectHeight = x.Rect.Height,
                                RectWidth = x.Rect.Width,
                                TagId = newTags[x.Tag],
                                Type = x.IsFace ? ImageObject.ObjectTypes.Face.ToString() : ImageObject.ObjectTypes.Object.ToString(),
                                Score = 0
                            }).ToList();

                            ScaleObjectRects(image, newObjects, bitmap);
                            foundFaces.AddRange(newObjects);
                        }
                    }
                    else
                    {
                        var accordwatch = new Stopwatch("AccordFaceDetect");

                        // Emgu isn't available, so use Accord.Net instead
                        var rects = _accordFaceService.DetectFaces(bitmap);

                        accordwatch.Stop();

                        if (rects.Any())
                        {
                            if (UseAzureForRecogition(rects))
                            {
                                useAzureDetection = true;
                            }
                            else
                            {
                                // Azure is disabled, so just use what we've got.
                                Logging.Log($" Accord.Net found {rects.Count} faces in {fileName}...");

                                var newTags = await CreateNewTags(rects);

                                var newObjects = rects.Select(x => new ImageObject
                                {
                                    ImageId = image.ImageId,
                                    RectX = x.Rect.Left,
                                    RectY = x.Rect.Top,
                                    RectHeight = x.Rect.Height,
                                    RectWidth = x.Rect.Width,
                                    Type = ImageObject.ObjectTypes.Face.ToString(), // Accord only does faces.
                                    TagId = newTags[x.Tag],
                                    RecogntionSource = ImageObject.RecognitionType.Accord,
                                    Score = 0
                                }).ToList();

                                ScaleObjectRects(image, newObjects, bitmap);
                                foundFaces.AddRange(newObjects);
                            }
                        }
                    }
                }

                if (useAzureDetection)
                {
                    var faceTag = await _metdataService.CreateTagsFromStrings(new List<string> { "Face" });
                    var faceTagId = faceTag.FirstOrDefault()?.TagId ?? 0;

                    var azurewatch = new Stopwatch("AzureFaceDetect");

                    Logging.LogVerbose($"Processing {medThumb.FullName} with Azure Face Service");

                    // We got predictions or we're scanning everything - so now let's try the image with Azure.
                    var azureFaces = await _azureFaceService.DetectFaces(bitmap);

                    azurewatch.Stop();

                    if (azureFaces.Any())
                    {
                        Logging.Log($" Azure found {azureFaces.Count} faces in {fileName}...");

                        // Get a list of the Azure Person IDs
                        var peopleIds = azureFaces.Select(x => x.PersonId.ToString());

                        // Create any new ones, or pull existing ones back from the cache
                        var people = await CreateMissingPeople(peopleIds);

                        var newObjects = azureFaces.Select(x => new ImageObject
                        {
                            ImageId = image.ImageId,
                            RectX = x.Left,
                            RectY = x.Top,
                            RectHeight = x.Height,
                            RectWidth = x.Width,
                            Type = ImageObject.ObjectTypes.Face.ToString(),
                            TagId = faceTagId,
                            RecogntionSource = ImageObject.RecognitionType.Azure,
                            Score = x.Score,
                            PersonId = _peopleCache[x.PersonId.ToString()].PersonId
                        }).ToList();

                        ScaleObjectRects(image, newObjects, bitmap);
                        foundFaces.AddRange(newObjects);

                        var peopleToAdd = foundFaces.Select(x => x.Person);

                        // Add them
                    }
                    else
                    {
                        // If we're scanning because local face detection found a face, log the result.
                        if (_azureFaceService.DetectionType == AzureFaceService.AzureDetection.ImagesWithFaces)
                            Logging.Log($"Azure found no faces in image {fileName}");
                        else
                            Logging.LogVerbose($"Azure found no faces in image {fileName}");
                    }
                }

                if (foundFaces.Any())
                {
                    // We've found some faces. Add a tagID.
                    const string faceTagName = "Face";
                    var tags = await _metdataService.CreateTagsFromStrings(new List<string> { faceTagName });
                    var faceTagId = tags.Single().TagId;
                    foundFaces.ForEach(x => x.TagId = faceTagId);
                }

                if (foundObjects.Any() || foundFaces.Any())
                {
                    var objWriteWatch = new Stopwatch("WriteDetectedObjects");

                    var allFound = foundObjects.Union(foundFaces).ToList();

                    using var db = new ImageContext();

                    // First, clear out the existing faces and objects - we don't want dupes
                    // TODO: Might need to be smarter about this once we add face names and
                    // Object identification details.
                    await db.BatchDelete(db.ImageObjects.Where(x => x.ImageId.Equals(image.ImageId)));
                    // Now add the objects and faces.
                    await db.BulkInsert(db.ImageObjects, allFound);

                    objWriteWatch.Stop();
                }
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception during AI detection for {fileName}: {ex}");
            }
        }


        /// <summary>
        /// Scales the detected face/object rectangles based on the full-sized image,
        /// since the object detection was done on a smaller thumbnail.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imgObjects">Collection of objects to scale</param>
        /// <param name="thumbSize"></param>
        private void ScaleObjectRects(Image image, List<ImageObject> imgObjects, System.Drawing.Bitmap bitmap)
        {
#pragma warning disable 1416
            var bmpHeight = bitmap.Height;
            var bmpWidth = bitmap.Width;
#pragma warning restore 1416

            if (bmpHeight == 0 || bmpWidth == 0)
                return;

            float longestBmpSide = bmpWidth > bmpHeight ? bmpWidth : bmpHeight;
            float longestImgSide = image.MetaData.Width > image.MetaData.Height ? image.MetaData.Width : image.MetaData.Height;
            var ratio = longestImgSide / longestBmpSide;

            foreach (var imgObj in imgObjects)
            {
                imgObj.RectX = (int)(imgObj.RectX * ratio);
                imgObj.RectY = (int)(imgObj.RectY * ratio);
                imgObj.RectWidth = (int)(imgObj.RectWidth * ratio);
                imgObj.RectHeight = (int)(imgObj.RectHeight * ratio);
            };
        }

        public void StartService()
        {
            if (!EnableImageRecognition)
            {
                Logging.Log("AI Image recognition service was disabled.");
                return;
            }

            LoadPersonCache();

            _workService.AddJobSource(this);
        }

        /// <summary>
        /// Work processing method for AI processing for a single
        /// Image.
        /// </summary>
        /// <param name="imageId"></param>
        /// <returns></returns>
        private async Task DetectObjects(int imageId)
        {
            var db = new ImageContext();

            var image = await _imageCache.GetCachedImage(imageId);
            db.Attach(image);

            await DetectObjects(image.MetaData);

            // The DetectObjects method will set the metadata AILastUpdated
            // timestamp. It may also update other fields.
            db.ImageMetaData.Update(image.MetaData);
            await db.SaveChangesAsync("UpdateAIGenDate");

            _imageCache.Evict(imageId);
        }

        public async Task MarkFolderForScan(Folder folder)
        {
            using var db = new ImageContext();

            var queryable = db.ImageMetaData.Where(img => img.Image.FolderId == folder.FolderId);

            int updated = await db.BatchUpdate(queryable, x => new ImageMetaData { AILastUpdated = null });
            _statusService.StatusText = $"Folder {folder.Name} ({updated} images) flagged for AI reprocessing.";

            _workService.HandleNewJobs(this);
        }

        public async Task MarkAllImagesForScan()
        {
            using var db = new ImageContext();

            int updated = await db.BatchUpdate(db.ImageMetaData, x => new ImageMetaData { AILastUpdated = null });

            _statusService.StatusText = $"All {updated} images flagged for AI reprocessing.";

            _workService.HandleNewJobs(this);
        }

        public async Task MarkImagesForScan(ICollection<Image> images)
        {
            using var db = new ImageContext();

            var ids = images.Select(x => x.ImageId).ToList();
            var queryable = db.ImageMetaData.Where(i => ids.Contains(i.ImageId));

            int rows = await db.BatchUpdate(queryable, x => new ImageMetaData { AILastUpdated = null });

            var msgText = rows == 1 ? $"Image {images.ElementAt(0).FileName}" : $"{rows} images";
            _statusService.StatusText = $"{msgText} flagged for AI reprocessing.";
        }

        public class AIProcess : IProcessJob
        {
            public int ImageId { get; set; }
            public ImageRecognitionService Service { get; set; }
            public string Description => "AI processing";
            public JobPriorities Priority => JobPriorities.ImageRecognition;

            public async Task Process()
            {
                await Service.DetectObjects(ImageId);
            }

            public bool CanProcess { get { return true; } }
        }

        public JobPriorities Priority => JobPriorities.ImageRecognition;

        public async Task<ICollection<IProcessJob>> GetPendingJobs( int maxJobs )
        {
            var db = new ImageContext();

            var images = await db.ImageMetaData.Where(x => x.AILastUpdated == null && x.ThumbLastUpdated != null)
                            .OrderByDescending(x => x.LastUpdated)
                            .Take(maxJobs)
                            .Select(x => x.ImageId)
                            .ToListAsync();

            if (images.Any())
            {
                var jobs = images.Select(x => new AIProcess { ImageId = x, Service = this })
                                .ToArray();
                return jobs;
            }

            return new AIProcess[0];
        }
    }
}
