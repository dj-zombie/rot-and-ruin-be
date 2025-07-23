using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities;
using RotAndRuin.Domain.Entities.Order;
using System.Linq.Expressions;

namespace RotAndRuin.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICartService _cartService;

    public OrderService(
        IApplicationDbContext context,
        IMapper mapper,
        ICartService cartService)
    {
        _context = context;
        _mapper = mapper;
        _cartService = cartService;
    }
    
    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items!)
            .ThenInclude(item => item.Product)
            .Include(o => o.ShippingAddress)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(string sessionId, string userId, ShippingAddressDto shippingAddressDto)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Cart is empty");

        var shippingAddress = _mapper.Map<ShippingAddress>(shippingAddressDto);
        
        var order = new Order
        {
            // UserId = userId,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order.Id = Guid.NewGuid();

        var orderItems = cart.Items.Select(cartItem => new OrderItem
        {
            OrderId = order.Id,
            Order = order,
            ProductId = cartItem.ProductId,
            Product = cartItem.Product,
            Quantity = cartItem.Quantity,
            PriceAtTimeOfPurchase = cartItem.Product.Price,
            ShippingPriceAtTimeOfPurchase = cartItem.Product.ShippingPrice,
            SubTotal = cartItem.Product.Price * cartItem.Quantity,
            ShippingTotal = cartItem.Product.ShippingPrice * cartItem.Quantity,
            Total = (cartItem.Product.Price * cartItem.Quantity) + (cartItem.Product.ShippingPrice * cartItem.Quantity),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        order.Items = orderItems;
        order.SubTotal = orderItems.Sum(item => item.SubTotal);
        order.ShippingTotal = orderItems.Sum(item => item.ShippingTotal);
        order.Total = orderItems.Sum(item => item.Total);

        _context.Orders.Add(order);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.ProductImages)
            .Include(o => o.ShippingAddress)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.ProductImages)
            .Include(o => o.ShippingAddress)
            .Where(o => o.UserId == Guid.Parse(userId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return _mapper.Map<OrderDto>(order);
    }
}