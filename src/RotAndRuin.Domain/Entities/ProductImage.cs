using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities;

public class ProductImage : BaseEntity
{
    public required string OriginalUrl { get; set; }
    // public required string ThumbnailUrl { get; set; }
    // public required string GridThumbnailUrl { get; set; }
    // public required string HighResolutionUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Optional metadata
    public string? AltText { get; set; }
    public string? Title { get; set; }
}