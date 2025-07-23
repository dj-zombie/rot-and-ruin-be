using RotAndRuin.Domain.Entities.Order;

namespace RotAndRuin.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
    public required ShippingAddressDto ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}