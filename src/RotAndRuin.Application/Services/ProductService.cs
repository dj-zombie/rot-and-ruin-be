using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.DTOs.Requests;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities;
namespace RotAndRuin.Application.Services;

public interface IProductService
{
    Task<IEnumerable<ProductGridDto>> GetProductsForGridAsync();
    Task<Product?> GetProductDetailsAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto> CreateProductAsync(
        ProductCreateRequest request, 
        List<(Stream Stream, string FileName, bool IsFeatured, string? AltText, string? Title, int DisplayOrder)> images = null);
}

public class ProductService(IApplicationDbContext context, IImageService imageService, IMapper mapper) : IProductService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IImageService _imageService = imageService;
    private readonly IMapper _mapper = mapper;

    public async Task<IEnumerable<ProductGridDto>> GetProductsForGridAsync()
    {
        var products = await _context.Products
            .Where(p => p.ProductVisible)
            .Include(p => p.ProductImages.Where(i => i.IsFeatured))
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductGridDto>>(products);
    }

    public async Task<Product?> GetProductDetailsAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.ProductImages.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .Include(p => p.ProductImages)
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> CreateProductAsync(
        ProductCreateRequest request, 
        List<(Stream Stream, string FileName, bool IsFeatured, string? AltText, string? Title, int DisplayOrder)> images = null)
    {
        var product = _mapper.Map<Product>(request);
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync(); // Save to get product ID

        if (images != null && images.Any())
        {
            var productImages = new List<ProductImage>();
            foreach (var (stream, fileName, isFeatured, altText, title, displayOrder) in images)
            {
                var image = await _imageService.ProcessAndUploadProductImageAsync(stream, fileName, isFeatured);
                image.ProductId = product.Id;
                image.AltText = altText;
                image.Title = title;
                image.DisplayOrder = displayOrder;
                Console.WriteLine($"Setting metadata for {fileName}: AltText={altText}, Title={title}, DisplayOrder={displayOrder}, IsFeatured={isFeatured}");
                productImages.Add(image);
            }

            await _context.ProductImages.AddRangeAsync(productImages);
            await _context.SaveChangesAsync();
            product.ProductImages = productImages;
            Console.WriteLine($"Associated {productImages.Count} images to product {product.Id}");
        }
        else
        {
            Console.WriteLine("No images provided.");
        }

        var productDto = _mapper.Map<ProductDto>(product);
        Console.WriteLine($"Mapped ProductDto with ID {productDto.Id}, Images count in DTO: {(productDto.ProductImages?.Count() ?? 0)}");
        return productDto;
    }

}