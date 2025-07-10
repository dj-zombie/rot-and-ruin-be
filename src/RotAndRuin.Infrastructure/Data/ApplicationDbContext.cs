using Microsoft.EntityFrameworkCore;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities;

namespace RotAndRuin.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Brand).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.ShippingPrice).HasPrecision(18, 2);
            entity.HasMany(p => p.ProductImages)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

        });
        
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ThumbnailUrl).IsRequired();
            entity.Property(e => e.GridThumbnailUrl).IsRequired();
        });

    }
}