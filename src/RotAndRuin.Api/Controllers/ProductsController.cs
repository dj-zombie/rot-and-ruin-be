using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Services;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Application.DTOs.Requests;
using Swashbuckle.AspNetCore.Annotations;

namespace RotAndRuin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController(
    IApplicationDbContext context, 
    IMapper mapper, 
    IProductService productService, 
    IImageService imageService
    ) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly IImageService _imageService = imageService;
    private readonly IApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;
    
    [HttpGet("grid")]
    public async Task<ActionResult<IEnumerable<ProductGridDto>>> GetProductGrid()
    {
        var products = await _productService.GetProductsForGridAsync();
        return Ok(products);
    }
   
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productService.GetProductDetailsAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        var productDto = _mapper.Map<ProductDto>(product); // If mapping is not done in service
        return Ok(productDto);
    }
    
    [HttpPost]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Create a new product with optional image uploads",
        Description = "Uploads product metadata and multiple images. Product metadata should be provided as JSON in the 'product' field.")]
    [SwaggerResponse(201, "Product created successfully", typeof(ProductDto))]
    [SwaggerResponse(400, "Invalid input data or empty/invalid files")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromForm] ProductCreateForm form)
    {
        if (form.Product == null)
            return BadRequest("Product metadata is missing or invalid");

        // Log received metadata to debug binding
        Console.WriteLine($"Received metadata counts: AltTexts={form.AltTexts?.Count ?? 0}, Titles={form.Titles?.Count ?? 0}, DisplayOrders={form.DisplayOrders?.Count ?? 0}, IsFeatureds={form.IsFeatureds?.Count ?? 0}");
        Console.WriteLine($"Received {form.Files?.Count ?? 0} files:");
        if (form.Files != null)
        {
            for (int i = 0; i < form.Files.Count; i++)
            {
                Console.WriteLine($"File[{i}]: {form.Files[i].FileName}");
            }
        }

        List<(Stream Stream, string FileName, bool IsFeatured, string? AltText, string? Title, int DisplayOrder)> images = new();
        if (form.Files != null && form.Files.Any())
        {
            for (int i = 0; i < form.Files.Count; i++)
            {
                var file = form.Files[i];
                if (file.Length == 0)
                    return BadRequest($"Empty file: {file.FileName}");
                if (!file.ContentType.StartsWith("image/"))
                    return BadRequest($"File is not an image: {file.FileName}");

                var stream = file.OpenReadStream();

                // Match metadata by index, if available
                string? altText = form.AltTexts != null && i < form.AltTexts.Count ? form.AltTexts[i] : null;
                string? title = form.Titles != null && i < form.Titles.Count ? form.Titles[i] : null;
                int displayOrder = form.DisplayOrders != null && i < form.DisplayOrders.Count ? form.DisplayOrders[i] : 0;
                bool isFeatured = form.IsFeatureds != null && i < form.IsFeatureds.Count ? form.IsFeatureds[i] : false;

                Console.WriteLine($"Mapping File[{i}] ({file.FileName}): AltText={altText}, Title={title}, DisplayOrder={displayOrder}, IsFeatured={isFeatured}");
                images.Add((stream, file.FileName, isFeatured, altText, title, displayOrder));
            }
        }

        var productDto = await _productService.CreateProductAsync(form.Product, images);

        // Dispose of streams after processing
        foreach (var (stream, _, _, _, _, _) in images)
        {
            await stream.DisposeAsync();
        }
        
        // Log raw form data for debugging
        foreach (var key in Request.Form.Keys)
        {
            Console.WriteLine($"Form Key: {key}, Value: {Request.Form[key]}");
        }

        return CreatedAtAction(nameof(GetProduct), new { id = productDto.Id }, productDto);
    }
    
    public class ProductCreateForm
    {
        [SwaggerSchema(Description = "Product metadata for creation")]
        public ProductCreateRequest Product { get; set; } = null!;

        [SwaggerSchema(Description = "List of image files to upload for the product")]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();

        [SwaggerSchema(Description = "List of AltText values corresponding to uploaded image files by index")]
        public List<string> AltTexts { get; set; } = new List<string>();

        [SwaggerSchema(Description = "List of Title values corresponding to uploaded image files by index")]
        public List<string> Titles { get; set; } = new List<string>();

        [SwaggerSchema(Description = "List of DisplayOrder values corresponding to uploaded image files by index")]
        public List<int> DisplayOrders { get; set; } = new List<int>();

        [SwaggerSchema(Description = "List of IsFeatured values corresponding to uploaded image files by index")]
        public List<bool> IsFeatureds { get; set; } = new List<bool>();
    }
}
