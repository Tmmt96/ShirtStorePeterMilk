using ShirtStore.Domain.Entities;

namespace ShirtStore.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Product>> GetAllPublishedAsync(CancellationToken ct = default);
    Task<List<Product>> SearchAsync(string query, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);
}
