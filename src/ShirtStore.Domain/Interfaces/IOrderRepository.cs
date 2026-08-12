using ShirtStore.Domain.Entities;

namespace ShirtStore.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<Order?> GetByCheckoutSessionIdAsync(string sessionId, CancellationToken ct = default);
    Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
    Task<List<Order>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    void Update(Order order);
}
