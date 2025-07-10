using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Products
{
    public class ProductImage : BaseEntity
    {
        public string OriginalUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string GridThumbnailUrl { get; set; } = string.Empty;
        public string HighResolutionUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsFeatured { get; set; }
        public string? AltText { get; set; }
        public string? Title { get; set; }
    }
}