using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Payments;

public class Payment : Entity
{
    public int SaleTransactionId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public required Money Amount { get; set; }
    public PaymentToken? ProviderToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
