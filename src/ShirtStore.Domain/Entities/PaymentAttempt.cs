using ShirtStore.Domain.Enums;

namespace ShirtStore.Domain.Entities;

public class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentStatus Status { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string? StripeChargeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? FailureMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
