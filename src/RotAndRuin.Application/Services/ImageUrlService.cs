using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace RotAndRuin.Application.Services;

public interface IImageUrlService
{
    string GetOptimizedImageUrl(string imagePath, int? width = null, int? height = null, string format = null);
}

public class ImageUrlService : IImageUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ImageUrlService> _logger;
    
    public ImageUrlService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ImageUrlService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    public string GetOptimizedImageUrl(string imagePath, int? width = null, int? height = null, string format = null)
    {
        try
        {
            // Extract the relative path from S3 URL if present
            if (imagePath.Contains("s3.amazonaws.com"))
            {
                var uri = new Uri(imagePath);
                var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
                // Find the index of "product-images" and take everything after it
                var productImagesIndex = Array.IndexOf(pathParts, "product-images");
                if (productImagesIndex >= 0)
                {
                    var relevantParts = pathParts.Skip(productImagesIndex);
                    imagePath = string.Join('/', relevantParts);
                }
                else
                {
                    imagePath = string.Join('/', pathParts.Skip(2));
                }

                // Decode any double-encoded segments
                imagePath = Uri.UnescapeDataString(imagePath);
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return $"/images/{imagePath}";
            }
        
            var scheme = request.Scheme;
            var host = request.Host.ToString();
            var baseUrl = $"{scheme}://{host}";
        
            var queryParams = new List<string>();
            if (width.HasValue) queryParams.Add($"width={width.Value}");
            if (height.HasValue) queryParams.Add($"height={height.Value}");
            if (!string.IsNullOrEmpty(format)) queryParams.Add($"format={format}");
        
            var query = queryParams.Any() ? $"?{string.Join("&", queryParams)}" : "";
        
            return $"{baseUrl}/images/{imagePath}{query}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating URL for path: {ImagePath}", imagePath);
            throw;
        }
    }
}