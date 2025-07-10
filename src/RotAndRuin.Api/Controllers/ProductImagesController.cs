using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotAndRuin.Application.Services;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Infrastructure.Data;

namespace RotAndRuin.Api.Controllers;

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