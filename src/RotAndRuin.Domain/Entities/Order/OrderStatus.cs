using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities.Order;

public enum OrderStatus
{
    Pending,
    PaymentReceived,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}