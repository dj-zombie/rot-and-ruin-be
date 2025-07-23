using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public bool IsAdmin { get; set; }
    public List<Order.Order> Orders { get; set; } = new();
}