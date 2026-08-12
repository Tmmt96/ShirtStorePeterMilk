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
        => _db.ProductVariants
            .Where(variant => variant.ProductId == productId)
            .OrderBy(variant => variant.Size == "S" ? 0
                : variant.Size == "M" ? 1
                : variant.Size == "L" ? 2
                : variant.Size == "XL" ? 3
                : 99)
            .ThenBy(variant => variant.Size)
            .ThenBy(variant => variant.Color)
            .ToListAsync(ct);

    public Task<List<ProductVariant>> GetBySkuAsync(string sku, CancellationToken ct = default)
        => _db.ProductVariants.Where(v => v.Sku == sku).ToListAsync(ct);

    public Task AddAsync(ProductVariant variant, CancellationToken ct = default)
    {
        _db.ProductVariants.Add(variant);
        return Task.CompletedTask;
    }

    public void Update(ProductVariant variant) => _db.ProductVariants.Update(variant);
}
