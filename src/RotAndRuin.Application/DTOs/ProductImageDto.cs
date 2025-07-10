namespace RotAndRuin.Application.DTOs;

public class ProductImageDto
{
    public Guid Id { get; init; }
    public string OriginalUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string GridThumbnailUrl { get; init; } = string.Empty;
    public string HighResolutionUrl { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsFeatured { get; init; }
    public string? AltText { get; init; }
    public string? Title { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}