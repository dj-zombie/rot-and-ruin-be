using Microsoft.Extensions.DependencyInjection;
using RotAndRuin.Domain.Entities;
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
            
            if (!context.Users.Any(u => u.IsAdmin))
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@rotandruin.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Change this!
                    IsAdmin = true
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while initializing the database: {ex.Message}");
            throw;
        }
    }
}