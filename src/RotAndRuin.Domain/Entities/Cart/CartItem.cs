using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Cart;

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public required Cart Cart { get; set; }
    public Guid ProductId { get; set; }
    public required Product Product { get; set; }
    public int Quantity { get; set; }
}