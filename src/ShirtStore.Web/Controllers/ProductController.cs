using Microsoft.AspNetCore.Mvc;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Web.Controllers;

/// <summary>
/// Página de detalhe de produto — server-rendered, SEO-first.
/// </summary>
public class ProductController : Controller
{
    private readonly IProductRepository      _products;
    private readonly IProductVariantRepository _variants;
    public ProductController(IProductRepository products, IProductVariantRepository variants)
    {
        _products  = products;
        _variants  = variants;
    }

    /// <summary>Página pública de uma camisola — URL /product/{slug}.</summary>
    [HttpGet("/product/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        var product = await _products.GetBySlugAsync(slug, ct);
        if (product is null || !product.Published)
            return NotFound();

        var variantList = await _variants.GetByProductIdAsync(product.Id, ct);
        var vm = new ProductDetailViewModel
        {
            Product  = product,
            Variants = variantList.Where(v => v.IsActive).ToList()
        };

        // SEO: meta tags preenchidas no layout a partir do ViewData
        ViewData["Title"]       = product.SeoTitle;
        ViewData["Description"] = product.SeoDescription;
        ViewData["Canonical"]   = product.CanonicalUrl;
        ViewData["NoIndex"]     = product.NoIndex;

        return View(vm);
    }
}

public record ProductDetailViewModel
{
    public Product Product { get; init; } = null!;
    public List<ProductVariant> Variants { get; init; } = new();
}
