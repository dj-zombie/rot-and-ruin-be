namespace RotAndRuin.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public required string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal ShippingPrice { get; set; }
        public bool ProductVisible { get; set; }
        public int StockLevel { get; set; }
        public required string CategoryDetails { get; set; } = string.Empty;
        public string MetaKeywords { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProductImageDto> ProductImages { get; set; } = new List<ProductImageDto>();
    }
}