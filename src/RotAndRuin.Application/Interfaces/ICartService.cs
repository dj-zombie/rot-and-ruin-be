using RotAndRuin.Application.DTOs;

namespace RotAndRuin.Application.Interfaces;
public interface ICartService
{
    Task<CartDto> GetCartAsync(string sessionId);
    Task<CartDto> UpdateCartAsync(CartDto cart);
    Task<CartDto> AddItemToCartAsync(string sessionId, Guid productId, int quantity);
    Task<CartDto> UpdateCartItemQuantityAsync(string sessionId, Guid productId, int quantity);
    Task<CartDto> RemoveItemFromCartAsync(string sessionId, Guid productId);
    Task ClearCartAsync(string sessionId);
}