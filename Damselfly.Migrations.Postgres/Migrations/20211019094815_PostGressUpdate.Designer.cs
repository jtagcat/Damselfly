﻿// <auto-generated />
using System;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Damselfly.Core.Migrations
{
    [DbContext(typeof(ImageContext))]
    [Migration("20211019094815_PostGressUpdate")]
    partial class PostGressUpdate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0-rc.2.21480.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Damselfly.Core.DbModels.AppIdentityUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<int?>("ApplicationRoleId")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationRoleId");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("Damselfly.Core.DbModels.ApplicationRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("Roles", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            ConcurrencyStamp = "d0cb56c4-daf0-45d2-bb3f-0e88cec24dcd",
                            Name = "User",
                            NormalizedName = "USER"
                        },
                        new
                        {
                            Id = 2,
                            ConcurrencyStamp = "197341df-56a0-4aef-ba40-6e7b3f7b04d5",
                            Name = "Admin",
                            NormalizedName = "ADMIN"
                        },
                        new
                        {
                            Id = 3,
                            ConcurrencyStamp = "f1513788-d2ab-4f8a-b706-aa780e58b643",
                            Name = "ReadOnly",
                            NormalizedName = "READONLY"
                        });
                });

            modelBuilder.Entity("Damselfly.Core.DbModels.ApplicationUserRole", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<int>("RoleId")
                        .HasColumnType("integer");

                    b.Property<int?>("ApplicationRoleId")
                        .HasColumnType("integer");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("ApplicationRoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("UserRoles", (string)null);
                });

            modelBuilder.Entity("Damselfly.Core.Models.Basket", b =>
                {
                    b.Property<int>("BasketId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("BasketId"));

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("BasketId");

                    b.HasIndex("UserId");

                    b.ToTable("Baskets");
                });

            modelBuilder.Entity("Damselfly.Core.Models.BasketEntry", b =>
                {
                    b.Property<int>("BasketEntryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("BasketEntryId"));

                    b.Property<int>("BasketId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("ImageId")
                        .HasColumnType("integer");

                    b.HasKey("BasketEntryId");

                    b.HasIndex("BasketId");

                    b.HasIndex("ImageId", "BasketId")
                        .IsUnique();

                    b.ToTable("BasketEntries");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Camera", b =>
                {
                    b.Property<int>("CameraId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("CameraId"));

                    b.Property<string>("Make")
                        .HasColumnType("text");

                    b.Property<string>("Model")
                        .HasColumnType("text");

                    b.Property<string>("Serial")
                        .HasColumnType("text");

                    b.HasKey("CameraId");

                    b.ToTable("Cameras");
                });

            modelBuilder.Entity("Damselfly.Core.Models.CloudTransaction", b =>
                {
                    b.Property<int>("CloudTransactionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("CloudTransactionId"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("TransCount")
                        .HasColumnType("integer");

                    b.Property<int>("TransType")
                        .HasColumnType("integer");

                    b.HasKey("CloudTransactionId");

                    b.HasIndex("Date", "TransType");

                    b.ToTable("CloudTransactions");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ConfigSetting", b =>
                {
                    b.Property<int>("ConfigSettingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ConfigSettingId"));

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("ConfigSettingId");

                    b.HasIndex("UserId");

                    b.ToTable("ConfigSettings");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ExifOperation", b =>
                {
                    b.Property<int>("ExifOperationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ExifOperationId"));

                    b.Property<int>("ImageId")
                        .HasColumnType("integer");

                    b.Property<int>("Operation")
                        .HasColumnType("integer");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("ExifOperationId");

                    b.HasIndex("TimeStamp");

                    b.HasIndex("UserId");

                    b.HasIndex("ImageId", "Text");

                    b.ToTable("KeywordOperations");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ExportConfig", b =>
                {
                    b.Property<int>("ExportConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ExportConfigId"));

                    b.Property<bool>("KeepFolders")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("Size")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<string>("WatermarkText")
                        .HasColumnType("text");

                    b.HasKey("ExportConfigId");

                    b.ToTable("DownloadConfigs");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Folder", b =>
                {
                    b.Property<int>("FolderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("FolderId"));

                    b.Property<DateTime?>("FolderScanDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("ParentFolderId")
                        .HasColumnType("integer");

                    b.Property<string>("Path")
                        .HasColumnType("text");

                    b.HasKey("FolderId");

                    b.HasIndex("FolderScanDate");

                    b.HasIndex("Path");

                    b.ToTable("Folders");
                });

            modelBuilder.Entity("Damselfly.Core.Models.FTSTag", b =>
                {
                    b.Property<int>("FTSTagId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("FTSTagId"));

                    b.Property<string>("Keyword")
                        .HasColumnType("text");

                    b.HasKey("FTSTagId");

                    b.ToTable("FTSTags");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Hash", b =>
                {
                    b.Property<int>("HashId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("HashId"));

                    b.Property<int>("ImageId")
                        .HasColumnType("integer");

                    b.Property<string>("MD5ImageHash")
                        .HasColumnType("text");

                    b.Property<string>("PerceptualHex1")
                        .HasColumnType("text");

                    b.Property<string>("PerceptualHex2")
                        .HasColumnType("text");

                    b.Property<string>("PerceptualHex3")
                        .HasColumnType("text");

                    b.Property<string>("PerceptualHex4")
                        .HasColumnType("text");

                    b.HasKey("HashId");

                    b.HasIndex("ImageId")
                        .IsUnique();

                    b.HasIndex("MD5ImageHash");

                    b.HasIndex("PerceptualHex1", "PerceptualHex2", "PerceptualHex3", "PerceptualHex4");

                    b.ToTable("Hashes");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Image", b =>
                {
                    b.Property<int>("ImageId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ImageId"));

                    b.Property<int?>("ClassificationId")
                        .HasColumnType("integer");

                    b.Property<double>("ClassificationScore")
                        .HasColumnType("double precision");

                    b.Property<DateTime>("FileCreationDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("FileLastModDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FileName")
                        .HasColumnType("text");

                    b.Property<int>("FileSizeBytes")
                        .HasColumnType("integer");

                    b.Property<int>("FolderId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("SortDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("ImageId");

                    b.HasIndex("FileLastModDate");

                    b.HasIndex("FileName");

                    b.HasIndex("FolderId");

                    b.HasIndex("LastUpdated");

                    b.HasIndex("SortDate");

                    b.HasIndex("FileName", "FolderId")
                        .IsUnique();

                    b.ToTable("Images");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageClassification", b =>
                {
                    b.Property<int>("ClassificationId")
                        .HasColumnType("integer");

                    b.Property<string>("Label")
                        .HasColumnType("text");

                    b.HasKey("ClassificationId");

                    b.HasIndex("Label")
                        .IsUnique();

                    b.ToTable("ImageClassifications");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageMetaData", b =>
                {
                    b.Property<int>("MetaDataId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("MetaDataId"));

                    b.Property<DateTime?>("AILastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("AverageColor")
                        .HasColumnType("text");

                    b.Property<int?>("CameraId")
                        .HasColumnType("integer");

                    b.Property<string>("Caption")
                        .HasColumnType("text");

                    b.Property<string>("Copyright")
                        .HasColumnType("text");

                    b.Property<string>("Credit")
                        .HasColumnType("text");

                    b.Property<DateTime>("DateTaken")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("DominantColor")
                        .HasColumnType("text");

                    b.Property<string>("Exposure")
                        .HasColumnType("text");

                    b.Property<string>("FNum")
                        .HasColumnType("text");

                    b.Property<bool>("FlashFired")
                        .HasColumnType("boolean");

                    b.Property<int>("Height")
                        .HasColumnType("integer");

                    b.Property<string>("ISO")
                        .HasColumnType("text");

                    b.Property<int>("ImageId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("LensId")
                        .HasColumnType("integer");

                    b.Property<int>("Rating")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("ThumbLastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Width")
                        .HasColumnType("integer");

                    b.HasKey("MetaDataId");

                    b.HasIndex("AILastUpdated");

                    b.HasIndex("CameraId");

                    b.HasIndex("DateTaken");

                    b.HasIndex("ImageId")
                        .IsUnique();

                    b.HasIndex("LensId");

                    b.HasIndex("ThumbLastUpdated");

                    b.ToTable("ImageMetaData");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageObject", b =>
                {
                    b.Property<int>("ImageObjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ImageObjectId"));

                    b.Property<int>("ImageId")
                        .HasColumnType("integer");

                    b.Property<int?>("PersonId")
                        .HasColumnType("integer");

                    b.Property<int>("RecogntionSource")
                        .HasColumnType("integer");

                    b.Property<int>("RectHeight")
                        .HasColumnType("integer");

                    b.Property<int>("RectWidth")
                        .HasColumnType("integer");

                    b.Property<int>("RectX")
                        .HasColumnType("integer");

                    b.Property<int>("RectY")
                        .HasColumnType("integer");

                    b.Property<double>("Score")
                        .HasColumnType("double precision");

                    b.Property<int>("TagId")
                        .HasColumnType("integer");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("ImageObjectId");

                    b.HasIndex("ImageId");

                    b.HasIndex("PersonId");

                    b.HasIndex("TagId");

                    b.ToTable("ImageObjects");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageTag", b =>
                {
                    b.Property<int>("ImageId")
                        .HasColumnType("integer");

                    b.Property<int>("TagId")
                        .HasColumnType("integer");

                    b.HasKey("ImageId", "TagId");

                    b.HasIndex("TagId");

                    b.HasIndex("ImageId", "TagId")
                        .IsUnique();

                    b.ToTable("ImageTags");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Lens", b =>
                {
                    b.Property<int>("LensId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("LensId"));

                    b.Property<string>("Make")
                        .HasColumnType("text");

                    b.Property<string>("Model")
                        .HasColumnType("text");

                    b.Property<string>("Serial")
                        .HasColumnType("text");

                    b.HasKey("LensId");

                    b.ToTable("Lenses");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Person", b =>
                {
                    b.Property<int>("PersonId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("PersonId"));

                    b.Property<string>("AzurePersonId")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.HasKey("PersonId");

                    b.HasIndex("State");

                    b.ToTable("People");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Tag", b =>
                {
                    b.Property<int>("TagId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("TagId"));

                    b.Property<bool>("Favourite")
                        .HasColumnType("boolean");

                    b.Property<string>("Keyword")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TagType")
                        .HasColumnType("integer");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("TagId");

                    b.HasIndex("Keyword")
                        .IsUnique()
                        .HasAnnotation("Npgsql:TsVectorConfig", "english");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("FriendlyName")
                        .HasColumnType("text");

                    b.Property<string>("Xml")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("DataProtectionKeys");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<int>("RoleId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("RoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("UserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("UserTokens", (string)null);
                });

            modelBuilder.Entity("Damselfly.Core.DbModels.AppIdentityUser", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.ApplicationRole", null)
                        .WithMany("AspNetUsers")
                        .HasForeignKey("ApplicationRoleId");
                });

            modelBuilder.Entity("Damselfly.Core.DbModels.ApplicationUserRole", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.ApplicationRole", null)
                        .WithMany("UserRoles")
                        .HasForeignKey("ApplicationRoleId");

                    b.HasOne("Damselfly.Core.DbModels.ApplicationRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Basket", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Damselfly.Core.Models.BasketEntry", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Basket", "Basket")
                        .WithMany("BasketEntries")
                        .HasForeignKey("BasketId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Damselfly.Core.Models.Image", "Image")
                        .WithMany("BasketEntries")
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Basket");

                    b.Navigation("Image");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ConfigSetting", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ExifOperation", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Image", "Image")
                        .WithMany()
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("Image");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Hash", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Image", "Image")
                        .WithOne("Hash")
                        .HasForeignKey("Damselfly.Core.Models.Hash", "ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Image");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Image", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Folder", "Folder")
                        .WithMany("Images")
                        .HasForeignKey("FolderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Folder");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageClassification", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Image", null)
                        .WithOne("Classification")
                        .HasForeignKey("Damselfly.Core.Models.ImageClassification", "ClassificationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageMetaData", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Camera", "Camera")
                        .WithMany()
                        .HasForeignKey("CameraId");

                    b.HasOne("Damselfly.Core.Models.Image", "Image")
                        .WithOne("MetaData")
                        .HasForeignKey("Damselfly.Core.Models.ImageMetaData", "ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Damselfly.Core.Models.Lens", "Lens")
                        .WithMany()
                        .HasForeignKey("LensId");

                    b.Navigation("Camera");

                    b.Navigation("Image");

                    b.Navigation("Lens");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageObject", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Image", "Image")
                        .WithMany("ImageObjects")
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Damselfly.Core.Models.Person", "Person")
                        .WithMany()
                        .HasForeignKey("PersonId");

                    b.HasOne("Damselfly.Core.Models.Tag", "Tag")
                        .WithMany("ImageObjects")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Image");

                    b.Navigation("Person");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("Damselfly.Core.Models.ImageTag", b =>
                {
                    b.HasOne("Damselfly.Core.Models.Image", "Image")
                        .WithMany("ImageTags")
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Damselfly.Core.Models.Tag", "Tag")
                        .WithMany("ImageTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Image");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.ApplicationRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("Damselfly.Core.DbModels.AppIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Damselfly.Core.DbModels.AppIdentityUser", b =>
                {
                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("Damselfly.Core.DbModels.ApplicationRole", b =>
                {
                    b.Navigation("AspNetUsers");

                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Basket", b =>
                {
                    b.Navigation("BasketEntries");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Folder", b =>
                {
                    b.Navigation("Images");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Image", b =>
                {
                    b.Navigation("BasketEntries");

                    b.Navigation("Classification");

                    b.Navigation("Hash");

                    b.Navigation("ImageObjects");

                    b.Navigation("ImageTags");

                    b.Navigation("MetaData");
                });

            modelBuilder.Entity("Damselfly.Core.Models.Tag", b =>
                {
                    b.Navigation("ImageObjects");

                    b.Navigation("ImageTags");
                });
#pragma warning restore 612, 618
        }
    }
}
