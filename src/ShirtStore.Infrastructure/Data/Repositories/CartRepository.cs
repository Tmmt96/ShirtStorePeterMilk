using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;
    public CartRepository(AppDbContext db) => _db = db;

    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => QueryWithItems().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cart?> GetByTokenAsync(string token, CancellationToken ct = default)
        => QueryWithItems().FirstOrDefaultAsync(c => c.CartToken == token, ct);

    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => QueryWithItems().FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public Task AddAsync(Cart cart, CancellationToken ct = default)
    {
        _db.Carts.Add(cart);
        return Task.CompletedTask;
    }

    public void AddItem(CartItem item) => _db.Entry(item).State = EntityState.Added;

    public void Update(Cart cart) => _db.Carts.Update(cart);

    private IQueryable<Cart> QueryWithItems()
        => _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant);
}
