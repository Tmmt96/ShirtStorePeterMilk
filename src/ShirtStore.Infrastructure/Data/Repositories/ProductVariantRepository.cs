using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _db;
    public ProductVariantRepository(AppDbContext db) => _db = db;

    public Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<List<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => _db.ProductVariants.Where(v => v.ProductId == productId).ToListAsync(ct);

    public Task<List<ProductVariant>> GetBySkuAsync(string sku, CancellationToken ct = default)
        => _db.ProductVariants.Where(v => v.Sku == sku).ToListAsync(ct);

    public Task AddAsync(ProductVariant variant, CancellationToken ct = default)
    {
        _db.ProductVariants.Add(variant);
        return Task.CompletedTask;
    }

    public void Update(ProductVariant variant) => _db.ProductVariants.Update(variant);
}
