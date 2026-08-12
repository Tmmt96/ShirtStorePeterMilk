namespace ShirtStore.Domain.Entities;

/// <summary>
/// Carrinho de compras — suporta utilizador autenticado e anónimo (cookie).
/// </summary>
public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? UserId    { get; set; }   // null = anónimo
    public string  CartToken { get; set; } = Guid.NewGuid().ToString("N"); // cookie

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new HashSet<CartItem>();
}
