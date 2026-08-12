using Microsoft.AspNetCore.Mvc;
using ShirtStore.Domain.Interfaces;

namespace ShirtStore.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductRepository _products;

    public HomeController(IProductRepository products) => _products = products;

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Camisolas portuguesas | Peter Milk";
        ViewData["Description"] = "Camisolas de alta qualidade. Compra online com envio para todo o país.";
        var products = await _products.GetAllPublishedAsync(ct);
        return View(products.OrderBy(p => p.CreatedAt).Take(6).ToList());
    }
}
