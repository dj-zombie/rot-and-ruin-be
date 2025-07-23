using RotAndRuin.Domain.Entities.Order;
using RotAndRuin.Application.DTOs;

namespace RotAndRuin.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(string sessionId, string userId, ShippingAddressDto shippingAddress);
    Task<OrderDto> GetOrderAsync(Guid orderId);
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}