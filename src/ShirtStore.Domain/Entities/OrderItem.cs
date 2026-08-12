namespace ShirtStore.Domain.Entities;

/// <summary>
/// Item de encomenda — guarda snapshot do produto para histórico imutável.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId       { get; set; }
    public string ProductName   { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }
    public string VariantSku     { get; set; } = string.Empty;
    public string VariantSize    { get; set; } = string.Empty;
    public string VariantColor   { get; set; } = string.Empty;

    public int    Quantity  { get; set; }
    public decimal UnitPrice { get; set; }   // preço no momento da encomenda
    public decimal LineTotal => Quantity * UnitPrice;
}
