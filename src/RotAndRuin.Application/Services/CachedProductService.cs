using Microsoft.Extensions.Caching.Distributed;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Domain.Entities;
using System.Text.Json;
using RotAndRuin.Application.DTOs.Requests;
using System.Collections.Generic;

namespace RotAndRuin.Application.Services
{
    public class CachedProductService : IProductService
    {
        private readonly IDistributedCache _cache;
        private readonly IProductService _productService;
        private const string ProductGridCacheKey = "product_grid";
        private const string AllProductsCacheKey = "all_products";
        private const string ProductDetailsCacheKeyPrefix = "product_details_";

        public CachedProductService(IDistributedCache cache, IProductService productService)
        {
            _cache = cache;
            _productService = productService;
        }

        public async Task<IEnumerable<ProductGridDto>> GetProductsForGridAsync()
        {
            var cached = await _cache.GetStringAsync(ProductGridCacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<ProductGridDto>>(cached) ?? Array.Empty<ProductGridDto>();
            }

            var products = await _productService.GetProductsForGridAsync();
            var serialized = JsonSerializer.Serialize(products);
            await _cache.SetStringAsync(ProductGridCacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return products;
        }

        public async Task<Product?> GetProductDetailsAsync(Guid id)
        {
            var cacheKey = $"{ProductDetailsCacheKeyPrefix}{id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<Product>(cached);
            }

            var product = await _productService.GetProductDetailsAsync(id);
            if (product != null)
            {
                var serialized = JsonSerializer.Serialize(product);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return product;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var cached = await _cache.GetStringAsync(AllProductsCacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<ProductDto>>(cached) ?? Array.Empty<ProductDto>();
            }

            var products = await _productService.GetAllProductsAsync();
            var serialized = JsonSerializer.Serialize(products);
            await _cache.SetStringAsync(AllProductsCacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return products;
        }

        public async Task<ProductDto> CreateProductAsync(
            ProductCreateRequest request, 
            List<(Stream Stream, string FileName, bool IsFeatured, string? AltText, string? Title, int DisplayOrder)> images = null)
        {
            // No caching for write operations; directly delegate to the underlying service
            var productDto = await _productService.CreateProductAsync(request, images);

            // Optionally, invalidate related caches after a write operation to ensure freshness
            await _cache.RemoveAsync(ProductGridCacheKey);
            await _cache.RemoveAsync(AllProductsCacheKey);
            // Note: We could also invalidate specific product details cache if needed, but it's optional

            return productDto;
        }
    }
}