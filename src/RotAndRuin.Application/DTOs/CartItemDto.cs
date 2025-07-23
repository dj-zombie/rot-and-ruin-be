namespace RotAndRuin.Application.DTOs;
public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string? OriginalUrl { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal ShippingPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
}