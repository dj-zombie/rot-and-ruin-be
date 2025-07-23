using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Cart;

public class Cart : BaseEntity
{
    public required string SessionId { get; set; }
    public string? UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}