using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotAndRuin.Application.Services;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Infrastructure.Data;

using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RotAndRuin.Api.Controllers;


[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly IImageUrlService _imageUrlService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IAmazonS3 s3Client,
        IImageUrlService imageUrlService,
        ILogger<ImagesController> logger)
    {
        _s3Client = s3Client;
        _imageUrlService = imageUrlService;
        _logger = logger;
    }

    [HttpGet("{**imagePath}")]
    public async Task<IActionResult> GetImage(
        string imagePath,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] string? format)
    {
        try
        {
            _logger.LogInformation("Received image request for path: {ImagePath}", imagePath);

            // Decode the URL-encoded path
            imagePath = Uri.UnescapeDataString(imagePath);
            _logger.LogInformation("Decoded path: {ImagePath}", imagePath);

            // Extract the relative path from S3 URL
            string relativeImagePath;
            if (imagePath.Contains("s3.amazonaws.com"))
            {
                var uri = new Uri(imagePath);
                // Split the path and remove empty entries
                var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Find the index of "product-images" and take everything after it
                var productImagesIndex = Array.IndexOf(pathParts, "product-images");
                if (productImagesIndex >= 0)
                {
                    var relevantParts = pathParts.Skip(productImagesIndex);
                    relativeImagePath = string.Join('/', relevantParts);
                }
                else
                {
                    // Fallback to original logic if "product-images" is not found
                    relativeImagePath = string.Join('/', pathParts.Skip(2));
                }

                // Decode any double-encoded segments (like %252F)
                relativeImagePath = Uri.UnescapeDataString(relativeImagePath);
            }
            else
            {
                relativeImagePath = imagePath;
            }

            _logger.LogInformation("Relative path: {RelativePath}", relativeImagePath);

            var bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");

            // Create the S3 request
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = relativeImagePath
            };

            _logger.LogInformation("Attempting to fetch from S3: Bucket={Bucket}, Key={Key}",
                bucketName, relativeImagePath);

            try
            {
                using var response = await _s3Client.GetObjectAsync(request);
                using var responseStream = response.ResponseStream;
                
                // Add cache headers
                Response.Headers.CacheControl = "public, max-age=604800"; // 7 days
                Response.Headers.Add("CDN-Cache-Control", "max-age=604800");
                Response.Headers.Add("Vary", "Accept"); // Important for content negotiation

                // Add ETag support
                var eTag = $"\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{imagePath}-{width}-{height}-{format}"))}\"";
                Response.Headers.ETag = eTag;

                // Check if-none-match
                var incomingETag = Request.Headers.IfNoneMatch.FirstOrDefault();
                if (incomingETag == eTag)
                {
                    return StatusCode(304); // Not Modified
                }

                // If no processing is needed, return the original
                if (!width.HasValue && !height.HasValue && string.IsNullOrEmpty(format))
                {
                    return File(responseStream, response.Headers.ContentType);
                }

                // Process the image using ImageSharp
                using var outputStream = new MemoryStream();
                using (var image = await Image.LoadAsync(responseStream))
                {
                    // Apply resizing if needed
                    if (width.HasValue || height.HasValue)
                    {
                        var resizeOptions = new ResizeOptions
                        {
                            Size = new Size(width ?? image.Width, height ?? image.Height),
                            Mode = ResizeMode.Max
                        };
                        image.Mutate(x => x.Resize(resizeOptions));
                    }

                    // Save with the specified format
                    switch (format?.ToLower())
                    {
                        case "webp":
                            await image.SaveAsWebpAsync(outputStream);
                            return File(outputStream.ToArray(), "image/webp");
                        case "jpg":
                        case "jpeg":
                            await image.SaveAsJpegAsync(outputStream);
                            return File(outputStream.ToArray(), "image/jpeg");
                        case "png":
                            await image.SaveAsPngAsync(outputStream);
                            return File(outputStream.ToArray(), "image/png");
                        default:
                            await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat);
                            return File(outputStream.ToArray(), response.Headers.ContentType);
                    }
                    
                }
                
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Image not found in S3: Bucket={Bucket}, Key={Key}",
                    bucketName, relativeImagePath);
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image: {ImagePath}, {Message}",
                imagePath, ex.Message);
            return StatusCode(500, "Error processing image");
        }
    }


    [HttpGet("url")]
    public IActionResult GetImageUrl(
        [FromQuery] string imagePath,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] string? format)
    {
        try
        {
            var url = _imageUrlService.GetOptimizedImageUrl(imagePath, width, height, format);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating URL: {ImagePath}, {Message}", imagePath, ex.Message);
            return BadRequest(new { error = "Invalid image path" });
        }
    }
}


[ApiController]
[Route("products/{productId}/images")]
public class ProductImagesController(
    IImageService imageService,
    IProductService productService,
    ApplicationDbContext context
    ) : ControllerBase
{
    private readonly IImageService _imageService = imageService;
    private readonly IProductService _productService = productService;
    private readonly ApplicationDbContext _context = context;

    [HttpPost]
    public async Task<IActionResult> UploadImage(
        Guid productId,
        IFormFile file,
        [FromQuery] bool isFeatured = false,
        [FromForm] string? altText = null,
        [FromForm] string? title = null,
        [FromForm] int displayOrder = 0)
    {
        if (file.Length == 0)
            return BadRequest("Empty file");

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("File is not an image");

        await using var stream = file.OpenReadStream();
        var image = await _imageService.ProcessAndUploadProductImageAsync(
            stream, 
            file.FileName, 
            isFeatured);

        image.ProductId = productId;
        image.AltText = altText;
        image.Title = title;
        image.DisplayOrder = displayOrder;
        await _context.ProductImages.AddAsync(image);
        await _context.SaveChangesAsync();

        return Ok(image);
    }

    [HttpDelete("{imageId}")]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(x => x.Id == imageId && x.ProductId == productId);
        
        if (image == null)
            return NotFound();

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{imageId}/order")]
    public async Task<IActionResult> UpdateImageOrder(
        Guid productId, 
        Guid imageId, 
        [FromBody] int newOrder)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(x => x.Id == imageId && x.ProductId == productId);
        
        if (image == null)
            return NotFound();

        image.DisplayOrder = newOrder;
        await _context.SaveChangesAsync();

        return Ok(image);
    }
}