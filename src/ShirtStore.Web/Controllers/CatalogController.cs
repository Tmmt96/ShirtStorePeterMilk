using Microsoft.AspNetCore.Mvc;
using ShirtStore.Domain.Interfaces;

namespace ShirtStore.Web.Controllers;

/// <summary>
/// Catálogo público — server-rendered para SEO.
/// </summary>
public class CatalogController : Controller
{
    private readonly IProductRepository _products;
    public CatalogController(IProductRepository products) => _products = products;

    /// <summary>Lista todas as camisolas publicadas.</summary>
    [HttpGet("/catalog")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}/catalog";
        var items = await _products.GetAllPublishedAsync(ct);
        return View(items.OrderBy(p => p.CreatedAt).ToList());
    }

    /// <summary>Pesquisa por frase/nome — server-rendered, noindex nos resultados.</summary>
    [HttpGet("/search")]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return RedirectToAction(nameof(Index));

        var results = await _products.SearchAsync(q, ct);
        ViewData["NoIndex"] = true;
        ViewData["SearchQuery"] = q;
        return View("Index", results);
    }
}
