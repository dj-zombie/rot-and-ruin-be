using Microsoft.Extensions.Logging;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace RotAndRuin.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger<ImageService> _logger;

        public ImageService(ICloudStorageService cloudStorageService, ILogger<ImageService> logger)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
        }

        public async Task<ProductImage> ProcessAndUploadProductImageAsync(Stream imageStream, string fileName, bool isFeatured)
        {
            using (var image = await Image.LoadAsync<Rgba32>(imageStream))
            {
                // Auto-orient based on EXIF data
                image.Mutate(x => x.AutoOrient());

                // Define max dimensions for the stored image
                int maxOriginalWidth = 2048;  // Slightly larger for better quality derivatives
                int maxOriginalHeight = 2048;

                if (image.Width > maxOriginalWidth || image.Height > maxOriginalHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxOriginalWidth, maxOriginalHeight),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3 // High quality resampling
                    }));
                }

                var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                var imageGuid = Guid.NewGuid();

                // Store as PNG for lossless quality if the image has transparency,
                // otherwise use JPEG for better compression
                var hasTransparency = await CheckTransparency(image);
                var format = hasTransparency ? "png" : "jpg";
                var mimeType = hasTransparency ? "image/png" : "image/jpeg";
                
                var s3Key = $"product-images/{imageGuid}/{baseFileName}.{format}";

                using (var outputStream = new MemoryStream())
                {
                    if (hasTransparency)
                    {
                        await image.SaveAsync(outputStream, new PngEncoder());
                    }
                    else
                    {
                        await image.SaveAsync(outputStream, new JpegEncoder { Quality = 90 });
                    }
                    
                    outputStream.Seek(0, SeekOrigin.Begin);

                    var s3OriginalUrl = await _cloudStorageService.UploadFileAsync(
                        outputStream,
                        s3Key,
                        mimeType
                    );

                    // Extract useful metadata
                    var metadata = ExtractImageMetadata(image);

                    var productImage = new ProductImage
                    {
                        OriginalUrl = s3Key,//s3OriginalUrl,
                        IsFeatured = isFeatured,
                        // Width = image.Width,
                        // Height = image.Height,
                        // Format = format,
                        // SizeInBytes = outputStream.Length,
                        // Store metadata that might be useful
                        // Metadata = metadata
                    };

                    _logger.LogInformation(
                        "Uploaded product image {ImageId} to {S3Key}. Size: {Width}x{Height}, Format: {Format}", 
                        imageGuid, s3Key, image.Width, image.Height, format);

                    return productImage;
                }
            }
        }

        private async Task<bool> CheckTransparency(Image<Rgba32> image)
        {
            // Quick check for transparency
            for (int y = 0; y < Math.Min(image.Height, 100); y += 10)
            {
                for (int x = 0; x < Math.Min(image.Width, 100); x += 10)
                {
                    if (image[x, y].A < 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Dictionary<string, string> ExtractImageMetadata(Image<Rgba32> image)
        {
            var metadata = new Dictionary<string, string>();
            
            // Extract EXIF data if available
            if (image.Metadata.ExifProfile != null)
            {
                var exif = image.Metadata.ExifProfile;
                
                // Camera make/model
                if (exif.TryGetValue(ExifTag.Make, out var make))
                    metadata["CameraMake"] = make?.Value?.ToString() ?? "";
                
                if (exif.TryGetValue(ExifTag.Model, out var model))
                    metadata["CameraModel"] = model?.Value?.ToString() ?? "";
                
                // Date taken
                if (exif.TryGetValue(ExifTag.DateTimeOriginal, out var dateTime))
                    metadata["DateTaken"] = dateTime?.Value?.ToString() ?? "";
            }
            
            return metadata;
        }
    }
}

/*using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace RotAndRuin.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger<ImageService> _logger;

        public ImageService(ICloudStorageService cloudStorageService, ILogger<ImageService> logger)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
        }

        public async Task<ProductImage> ProcessAndUploadProductImageAsync(Stream imageStream, string fileName, bool isFeatured)
        {
            using (var image = await Image.LoadAsync<Rgba32>(imageStream))
            {
                // Auto-orient based on EXIF data
                image.Mutate(x => x.AutoOrient());

                // Define max dimensions for the stored image
                int maxOriginalWidth = 2048;  // Slightly larger for better quality derivatives
                int maxOriginalHeight = 2048;

                if (image.Width > maxOriginalWidth || image.Height > maxOriginalHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxOriginalWidth, maxOriginalHeight),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3 // High quality resampling
                    }));
                }

                var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                var imageGuid = Guid.NewGuid();

                // Store as PNG for lossless quality if the image has transparency,
                // otherwise use JPEG for better compression
                var hasTransparency = await CheckTransparency(image);
                var format = hasTransparency ? "png" : "jpg";
                var mimeType = hasTransparency ? "image/png" : "image/jpeg";
                
                var s3Key = $"product-images/{imageGuid}/{baseFileName}.{format}";

                using (var outputStream = new MemoryStream())
                {
                    if (hasTransparency)
                    {
                        await image.SaveAsync(outputStream, new PngEncoder());
                    }
                    else
                    {
                        await image.SaveAsync(outputStream, new JpegEncoder { Quality = 90 });
                    }
                    
                    outputStream.Seek(0, SeekOrigin.Begin);

                    var s3OriginalUrl = await _cloudStorageService.UploadFileAsync(
                        outputStream,
                        s3Key,
                        mimeType
                    );

                    // Extract useful metadata
                    var metadata = ExtractImageMetadata(image);

                    var productImage = new ProductImage
                    {
                        OriginalUrl = s3OriginalUrl,
                        IsFeatured = isFeatured,
                        Width = image.Width,
                        Height = image.Height,
                        Format = format,
                        SizeInBytes = outputStream.Length,
                        // Store metadata that might be useful
                        Metadata = metadata
                    };

                    _logger.LogInformation(
                        "Uploaded product image {ImageId} to {S3Key}. Size: {Width}x{Height}, Format: {Format}", 
                        imageGuid, s3Key, image.Width, image.Height, format);

                    return productImage;
                }
            }
        }

        private async Task<bool> CheckTransparency(Image<Rgba32> image)
        {
            // Quick check for transparency
            for (int y = 0; y < Math.Min(image.Height, 100); y += 10)
            {
                for (int x = 0; x < Math.Min(image.Width, 100); x += 10)
                {
                    if (image[x, y].A < 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Dictionary<string, string> ExtractImageMetadata(Image<Rgba32> image)
        {
            var metadata = new Dictionary<string, string>();
            
            // Extract EXIF data if available
            if (image.Metadata.ExifProfile != null)
            {
                var exif = image.Metadata.ExifProfile;
                
                // Camera make/model
                if (exif.TryGetValue(ExifTag.Make, out var make))
                    metadata["CameraMake"] = make?.Value?.ToString() ?? "";
                
                if (exif.TryGetValue(ExifTag.Model, out var model))
                    metadata["CameraModel"] = model?.Value?.ToString() ?? "";
                
                // Date taken
                if (exif.TryGetValue(ExifTag.DateTimeOriginal, out var dateTime))
                    metadata["DateTaken"] = dateTime?.Value?.ToString() ?? "";
            }
            
            return metadata;
        }
    }
}*/