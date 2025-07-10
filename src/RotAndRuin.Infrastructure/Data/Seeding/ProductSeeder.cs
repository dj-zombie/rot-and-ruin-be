using RotAndRuin.Domain.Entities;

namespace RotAndRuin.Infrastructure.Data.Seeding;

public static class ProductSeeder
{
    public static List<Product> GetInitialProducts()
    {
        return new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "DEATH SWIFT",
                Brand = "ZOMBIE",
                Description = "Not your average Taylor Swift merch—this is DEATH SWIFT. A black sleeveless shirt, reimagined with death metal ferocity. Distressed, cut-up, and shredded like it's survived the pit.",
                Price = 29.99m,
                ShippingPrice = 5.99m,
                ProductVisible = true,
                StockLevel = 100,
                CategoryDetails = "Men's Clothing",
                MetaKeywords = "death metal, Taylor Swift, merch, distressed, sleeveless",
                MetaDescription = "Death metal reimagining of Taylor Swift merch. Distressed sleeveless shirt with metal-style lettering.",
                ProductUrl = "/products/death-swift",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProductImages = new List<ProductImage>
                {
                    new ProductImage
                    {
                        OriginalUrl = "https://example.com/images/death-swift-original.jpg",
                        ThumbnailUrl = "https://example.com/images/death-swift-thumbnail.jpg",
                        GridThumbnailUrl = "https://example.com/images/death-swift-grid.jpg",
                        HighResolutionUrl = "https://example.com/images/death-swift-hires.jpg",
                        DisplayOrder = 1,
                        IsFeatured = true,
                        AltText = "Death Swift Sleeveless Shirt",
                        Title = "Death Swift"
                    }
                }
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "SACRIFICE IN THE COURT",
                Brand = "ZOMBIE",
                Description = "Dominate the court, summon the inferno—SACRIFICE IN THE COURT is a pair of red basketball shorts made for the bold. Crafted for comfort and breathability.",
                Price = 34.99m,
                ShippingPrice = 5.99m,
                ProductVisible = true,
                StockLevel = 75,
                CategoryDetails = "Men's Clothing",
                MetaKeywords = "basketball shorts, ZOMBIE, death metal, athletic wear",
                MetaDescription = "Red basketball shorts with custom ZOMBIE death metal logo and satanic sigils. Perfect for court or street.",
                ProductUrl = "/products/sacrifice-in-the-court",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProductImages = new List<ProductImage>
                {
                    new ProductImage
                    {
                        OriginalUrl = "https://example.com/images/sacrifice-in-the-court-original.jpg",
                        ThumbnailUrl = "https://example.com/images/sacrifice-in-the-court-thumbnail.jpg",
                        GridThumbnailUrl = "https://example.com/images/sacrifice-in-the-court-grid.jpg",
                        HighResolutionUrl = "https://example.com/images/sacrifice-in-the-court-hires.jpg",
                        DisplayOrder = 1,
                        IsFeatured = true,
                        AltText = "Sacrifice in the Court Basketball Shorts",
                        Title = "Sacrifice in the Court"
                    }
                }
            }
        };
    }
}