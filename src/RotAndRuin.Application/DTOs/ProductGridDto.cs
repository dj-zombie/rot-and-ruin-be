namespace RotAndRuin.Application.DTOs;

public class ProductGridDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ProductUrl { get; set; }
    public bool ProductVisible { get; set; }
    public decimal Price { get; set; }
    public string? GridThumbnailUrl { get; set; }
}