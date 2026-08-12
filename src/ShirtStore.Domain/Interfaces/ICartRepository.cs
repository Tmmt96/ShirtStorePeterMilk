using ShirtStore.Domain.Entities;

namespace ShirtStore.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cart?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<Cart?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
    void Update(Cart cart);
}
