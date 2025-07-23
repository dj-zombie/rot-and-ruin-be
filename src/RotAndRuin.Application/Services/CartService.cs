using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities.Cart;

namespace RotAndRuin.Application.Services;

public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CartService> _logger;

    public CartService(IApplicationDbContext context, IMapper mapper, ILogger<CartService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(string cartId)
    {
        try
        {
            _logger.LogInformation($"Fetching cart with ID: {cartId}");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(c => c.Id.ToString() == cartId);

            if (cart == null)
            {
                _logger.LogWarning($"Cart not found with ID: {cartId}");
                return null;
            }

            if (cart.Items == null)
            {
                _logger.LogWarning($"Cart items collection is null for cart: {cartId}");
                cart.Items = new List<CartItem>();
            }

            _logger.LogInformation($"Found cart with {cart.Items.Count} items");

            var cartDto = _mapper.Map<CartDto>(cart);
        
            _logger.LogInformation($"Mapped cart DTO with {cartDto.Items?.Count ?? 0} items");
        
            return cartDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching cart {cartId}");
            throw;
        }
    }
    
    public async Task<CartDto> UpdateCartAsync(CartDto cartDto)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.Id == cartDto.Id || c.SessionId == cartDto.Id.ToString());

        if (cart == null)
        {
            throw new Exception($"Cart not found: {cartDto.Id}");
        }

        // Update cart properties
        cart.PaymentIntentId = cartDto.PaymentIntentId;
        cart.ClientSecret = cartDto.ClientSecret;
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetCartAsync(cartDto.Id.ToString());
    }

    public async Task<CartDto> AddItemToCartAsync(string cartId, Guid productId, int quantity)
    {
        _logger.LogInformation($"Adding item to cart. Cart ID: {cartId}, Product ID: {productId}");

        // Step 1: Find the cart by its Primary Key.
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id.ToString() == cartId);

        // Step 2: If it doesn't exist, create it using the ID from the session.
        if (cart == null)
        {
            _logger.LogInformation($"Cart {cartId} not found. Creating a new cart.");
            cart = new Cart
            {
                // CRITICAL: The Id is now set explicitly from the session-managed GUID.
                Id = Guid.Parse(cartId),
                SessionId = cartId, 
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>()
            };
            _context.Carts.Add(cart);
        }

        // Step 3: Handle product logic (this part was already good).
        var product = await _context.Products.FindAsync(productId);
        if (product == null) throw new KeyNotFoundException($"Product with ID {productId} not found.");
        if (product.StockLevel < quantity) throw new InvalidOperationException("Insufficient stock.");

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            if (product.StockLevel < existingItem.Quantity + quantity)
            {
                throw new InvalidOperationException($"Insufficient stock. Only {product.StockLevel} available.");
            }
            existingItem.Quantity += quantity;
        }
        else
        {
            var newCartItem = new CartItem
            {
                ProductId = productId,
                Quantity = quantity,
                Cart = cart,
                Product = product 
            };
            cart.Items.Add(newCartItem);
        }

        // Step 4: Save everything.
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Successfully saved changes for cart {cartId}.");

        // Step 5: Return the full DTO.
        // Re-querying ensures all navigation properties are loaded for the mapper.
        var updatedCart = await GetCartAsync(cartId);
        return updatedCart;
    }

    public async Task<CartDto> UpdateCartItemQuantityAsync(string sessionId, Guid productId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item == null)
            throw new KeyNotFoundException($"Product with ID {productId} not found in cart.");

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.StockLevel < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {product?.StockLevel ?? 0}");

            item.Quantity = quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return _mapper.Map<CartDto>(cart);
    }

    public async Task<CartDto> RemoveItemFromCartAsync(string sessionId, Guid productId)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return _mapper.Map<CartDto>(cart);
    }

    public async Task ClearCartAsync(string sessionId)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<Cart> GetOrCreateCartAsync(string sessionId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (cart == null)
        {
            cart = new Cart
            {
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<CartItem>()
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

}