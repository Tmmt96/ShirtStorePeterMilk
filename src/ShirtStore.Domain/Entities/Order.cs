using ShirtStore.Domain.Enums;

namespace ShirtStore.Domain.Entities;

/// <summary>
/// Encomenda — estado completo, endereços e histórico de pagamento.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OrderNumber { get; set; } = string.Empty;   // ORD-YYYYMMDD-XXXX

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string  CustomerEmail    { get; set; } = string.Empty;
    public string  CustomerName     { get; set; } = string.Empty;
    public string? CustomerPhone    { get; set; }

    // Endereço de faturação
    public string BillingAddressLine1 { get; set; } = string.Empty;
    public string? BillingAddressLine2 { get; set; }
    public string BillingCity          { get; set; } = string.Empty;
    public string BillingPostalCode    { get; set; } = string.Empty;
    public string BillingCountry       { get; set; } = string.Empty;
    public string? BillingTaxId        { get; set; }

    // Endereço de envio (pode ser igual ao de faturação)
    public string ShippingAddressLine1 { get; set; } = string.Empty;
    public string? ShippingAddressLine2 { get; set; }
    public string ShippingCity          { get; set; } = string.Empty;
    public string ShippingPostalCode    { get; set; } = string.Empty;
    public string ShippingCountry       { get; set; } = string.Empty;

    // Totais
    public decimal SubTotal   { get; set; }
    public decimal Shipping   { get; set; }
    public decimal Tax        { get; set; }
    public decimal Total      { get; set; }
    public string  Currency   { get; set; } = "EUR";

    // Estado
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    // Stripe
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();
    public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new HashSet<PaymentAttempt>();
}
