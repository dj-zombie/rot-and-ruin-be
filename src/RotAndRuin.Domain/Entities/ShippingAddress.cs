using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities;

public class ShippingAddress : BaseEntity
{
    public required string FullName { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string PostalCode { get; set; }
    public required string Country { get; set; }
}