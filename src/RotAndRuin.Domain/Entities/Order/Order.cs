using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Order;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public string? PaymentIntentId { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
    public required ShippingAddress ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

