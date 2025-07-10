using Microsoft.EntityFrameworkCore;
using RotAndRuin.Domain.Entities;

namespace RotAndRuin.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; set; }
    DbSet<ProductImage> ProductImages { get; } 
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}