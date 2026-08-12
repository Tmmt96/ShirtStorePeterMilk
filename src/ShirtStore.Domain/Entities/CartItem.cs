namespace ShirtStore.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CartId { get; set; }
    public Cart  Cart   { get; set; } = null!;

    public Guid ProductId       { get; set; }
    public Product Product      { get; set; } = null!;

    public Guid ProductVariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public int    Quantity { get; set; }
    public decimal UnitPrice { get; set; }   // snapshot no momento de adicionar

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
