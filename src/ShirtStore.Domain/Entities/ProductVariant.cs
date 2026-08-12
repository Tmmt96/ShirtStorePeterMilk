namespace ShirtStore.Domain.Entities;

/// <summary>
/// Variante de uma camisola: combinação de tamanho + cor com preço e stock próprios.
/// </summary>
public class ProductVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Sku        { get; set; } = string.Empty;   // SKU único
    public string Size       { get; set; } = string.Empty;   // S, M, L, XL, XXL
    public string Color      { get; set; } = string.Empty;   // Preto, Branco, Azul
    public string? ColorHex  { get; set; }                   // #000000

    public decimal Price    { get; set; }                    // preço efetivo desta variante
    public int     Stock    { get; set; }                    // stock disponível
    public int     Reserved { get; set; } = 0;               // stock reservado por carrinhos

    public bool    IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Stock disponível para venda.</summary>
    public int AvailableStock => Math.Max(0, Stock - Reserved);
}
