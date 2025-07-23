using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Order;
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public required Order Order { get; set; }
    public Guid ProductId { get; set; }
    public required Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtTimeOfPurchase { get; set; }
    public decimal ShippingPriceAtTimeOfPurchase { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
}