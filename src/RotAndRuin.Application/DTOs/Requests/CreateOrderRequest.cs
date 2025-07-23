using RotAndRuin.Domain.Entities;

namespace RotAndRuin.Application.DTOs.Requests;

public class CreateOrderRequest
{
    public required ShippingAddressDto ShippingAddress { get; set; }
}