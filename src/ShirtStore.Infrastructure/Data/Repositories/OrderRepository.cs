using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Items).Include(o => o.PaymentAttempts)
             .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

    public Task<Order?> GetByCheckoutSessionIdAsync(string sessionId, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Items)
             .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == sessionId, ct);

    public Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Items)
             .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId, ct);

    public Task<List<Order>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => _db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt)
             .ToListAsync(ct);

    public Task AddAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public void Update(Order order) => _db.Orders.Update(order);
}
