namespace ShirtStore.Domain.Entities;

/// <summary>
/// Camisola — conteúdo editorial gerido no Umbraco, dados transacionais guardados aqui.
/// </summary>
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Conteúdo editorial (sincronizado do Umbraco) ───────────────────────
    public string Name        { get; set; } = string.Empty;
    public string Slug        { get; set; } = string.Empty;   // URL amigável única
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl   { get; set; }
    public string? Category   { get; set; }
    public string? Tags       { get; set; }   // CSV

    // ── SEO ────────────────────────────────────────────────────────────────
    public string  SeoTitle       { get; set; } = string.Empty;
    public string? SeoDescription { get; set; }
    public string? CanonicalUrl   { get; set; }
    public bool   NoIndex         { get; set; } = false;

    // ── Preço base (a variante pode sobrepor) ───────────────────────────────
    public decimal BasePrice { get; set; }
    public string  Currency  { get; set; } = "EUR";

    // ── Estado ─────────────────────────────────────────────────────────────
    public bool   Published { get; set; } = false;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt  { get; set; } = DateTime.UtcNow;

    // ── Relações ───────────────────────────────────────────────────────────
    public ICollection<ProductVariant> Variants { get; set; } = new HashSet<ProductVariant>();
    public ICollection<OrderItem>      OrderItems { get; set; } = new HashSet<OrderItem>();
}
