using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Products.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<List<Product>> GetAllPublishedAsync(CancellationToken ct = default)
        => _db.Products.Where(p => p.Published).ToListAsync(ct);

    public Task<List<Product>> SearchAsync(string query, CancellationToken ct = default)
    {
        var q = query.Trim().ToLower();
        return _db.Products
            .Where(p => p.Published &&
                        (p.Name.ToLower().Contains(q) ||
                         (p.Description != null && p.Description.ToLower().Contains(q)) ||
                         (p.Tags != null && p.Tags.ToLower().Contains(q))))
            .ToListAsync(ct);
    }

    public Task AddAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Add(product);
        return Task.CompletedTask;
    }

    public void Update(Product product) => _db.Products.Update(product);
}
