namespace RotAndRuin.Application.DTOs.Requests;

using RotAndRuin.Application.DTOs;

public class ProductCreateRequest
{
    public required string Name { get; set; }
    public required string Brand { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public decimal ShippingPrice { get; set; }
    public bool ProductVisible { get; set; }
    public int StockLevel { get; set; }
    public required string CategoryDetails { get; set; }
    public required string MetaKeywords { get; set; }
    public required string MetaDescription { get; set; }
    public required string ProductUrl { get; set; }
}