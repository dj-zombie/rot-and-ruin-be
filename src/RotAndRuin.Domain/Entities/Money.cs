using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities;

public class Money : BaseEntity
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}