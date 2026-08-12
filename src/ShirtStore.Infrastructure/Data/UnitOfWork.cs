using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public IProductRepository      Products      => _products ??= new ProductRepository(_db);
    public IProductVariantRepository Variants   => _variants ??= new ProductVariantRepository(_db);
    public ICartRepository         Carts         => _carts ??= new CartRepository(_db);
    public IOrderRepository        Orders        => _orders ??= new OrderRepository(_db);

    private IProductRepository?      _products;
    private IProductVariantRepository? _variants;
    private ICartRepository?         _carts;
    private IOrderRepository?        _orders;

    public UnitOfWork(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    public void Dispose() => _db.Dispose();
}
