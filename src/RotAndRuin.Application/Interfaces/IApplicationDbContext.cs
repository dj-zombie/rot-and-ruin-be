using Microsoft.EntityFrameworkCore;
using RotAndRuin.Domain.Entities;
using RotAndRuin.Domain.Entities.Cart;
using RotAndRuin.Domain.Entities.Order;

namespace RotAndRuin.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; set; }
    DbSet<ProductImage> ProductImages { get; } 
    DbSet<Cart> Carts { get; set; }
    DbSet<CartItem> CartItems { get; set; }
    DbSet<Order> Orders { get; set; }
    DbSet<OrderItem> OrderItems { get; set; }
    DbSet<ShippingAddress> ShippingAddresses { get; set; }
    DbSet<User> Users { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}