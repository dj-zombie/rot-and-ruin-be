using RotAndRuin.Application.DTOs;
using RotAndRuin.Domain.Entities.Order;

namespace RotAndRuin.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentDto> CreateOrUpdatePaymentIntent(string cartId);
    Task<Order> UpdateOrderPaymentSucceeded(string paymentIntentId);
    Task<string> CreateCheckoutSession(string cartId, string userId, ShippingAddressDto shippingAddress);
    Task HandleWebhook(string json, string stripeSignature);
}
