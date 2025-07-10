using Microsoft.Extensions.DependencyInjection;
using RotAndRuin.Infrastructure.Data.Seeding;

namespace RotAndRuin.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            if (!context.Products.Any())
            {
                var products = ProductSeeder.GetInitialProducts();
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
                Console.WriteLine("Database seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while initializing the database: {ex.Message}");
            throw;
        }
    }
}