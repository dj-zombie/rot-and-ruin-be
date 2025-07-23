namespace RotAndRuin.Application.DTOs.Requests;

public class AddCartItemRequest
{
    public required Guid ProductId { get; set; }
    public required int Quantity { get; set; }
}