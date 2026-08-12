namespace ShirtStore.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository      Products      { get; }
    IProductVariantRepository Variants   { get; }
    ICartRepository         Carts         { get; }
    IOrderRepository        Orders        { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
