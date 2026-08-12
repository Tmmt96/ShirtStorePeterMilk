using ShirtStore.Domain.Entities;

namespace ShirtStore.Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<List<ProductVariant>> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(ProductVariant variant, CancellationToken ct = default);
    void Update(ProductVariant variant);
}
