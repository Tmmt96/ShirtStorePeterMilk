using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;
    public CartRepository(AppDbContext db) => _db = db;

    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cart?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CartToken == token, ct);

    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public Task AddAsync(Cart cart, CancellationToken ct = default)
    {
        _db.Carts.Add(cart);
        return Task.CompletedTask;
    }

    public void Update(Cart cart) => _db.Carts.Update(cart);
}
