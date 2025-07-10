using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Products
{
    public class ProductVariant : BaseEntity
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public decimal Price { get; set; }
        public int StockLevel { get; set; }
        
        // Navigation property
        public Product Product { get; set; } = null!;
    }
}