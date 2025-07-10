// src/RotAndRuin.Infrastructure/Services/ImageService.cs
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RotAndRuin.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly ICloudStorageService _cloudStorageService;

        public ImageService(ICloudStorageService cloudStorageService)
        {
            _cloudStorageService = cloudStorageService;
        }

        public async Task<ProductImage> ProcessAndUploadProductImageAsync(Stream imageStream, string fileName, bool isFeatured)
        {
            // Load the image directly as Rgba32 if possible
            using (var image = await Image.LoadAsync<Rgba32>(imageStream))
            {
                // Define target sizes for different image versions
                var imageVersions = new[]
                {
                    new { Name = "original", Size = new Size(image.Width, image.Height) }, // Original size
                    new { Name = "thumbnail", Size = new Size(100, 100) },                // Thumbnail
                    new { Name = "grid", Size = new Size(200, 200) },                     // Grid Thumbnail
                    new { Name = "hires", Size = new Size(800, 800) }                     // High Resolution
                };

                var urls = new string[4]; // Store URLs for each version

                for (int i = 0; i < imageVersions.Length; i++)
                {
                    var version = imageVersions[i];
                    using (var processedImage = image.Clone()) // Clone the Rgba32 image
                    {
                        if (version.Name != "original")
                        {
                            processedImage.Mutate(x => x
                                .Resize(new ResizeOptions
                                {
                                    Size = version.Size,
                                    Mode = ResizeMode.Max // Maintain aspect ratio, fit within bounds
                                }));
                        }

                        var processedFileName = version.Name == "original"
                            ? $"product-images/{version.Name}/{fileName}"
                            : $"product-images/{version.Name}/{Path.GetFileNameWithoutExtension(fileName)}_{version.Size.Width}x{version.Size.Height}{Path.GetExtension(fileName)}";

                        using (var outputStream = new MemoryStream())
                        {
                            // Save processed image to memory stream
                            await processedImage.SaveAsync(outputStream, processedImage.Metadata.DecodedImageFormat);
                            outputStream.Seek(0, SeekOrigin.Begin);

                            // Upload to S3
                            urls[i] = await _cloudStorageService.UploadFileAsync(
                                outputStream,
                                processedFileName,
                                processedImage.Metadata.DecodedImageFormat?.DefaultMimeType ?? "image/jpeg");
                        }
                    }
                }

                var productImage = new ProductImage
                {
                    OriginalUrl = urls[0],       // Original image URL
                    ThumbnailUrl = urls[1],      // Thumbnail URL
                    GridThumbnailUrl = urls[2],  // Grid Thumbnail URL
                    HighResolutionUrl = urls[3], // High Resolution URL
                    IsFeatured = isFeatured
                };

                return productImage;
            }
        }
    }
}