using Microsoft.AspNetCore.Mvc;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Web.Controllers;

/// <summary>
/// Carrinho de compras — suporta anónimo e autenticado.
/// </summary>
public class CartController : Controller
{
    private readonly IUnitOfWork _uow;
    public CartController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Página do carrinho — noindex.</summary>
    [HttpGet("/cart")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(ct);
        ViewData["NoIndex"] = true;
        ViewData["Title"]   = "Carrinho";
        return View(cart);
    }

    /// <summary>Adiciona variante ao carrinho.</summary>
    [HttpPost("/cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromForm] Guid variantId, [FromForm] int quantity = 1, CancellationToken ct = default)
    {
        if (quantity < 1) quantity = 1;

        var cart = await GetOrCreateCartAsync(ct);
        var variant = await _uow.Variants.GetByIdAsync(variantId, ct);
        if (variant is null || !variant.IsActive || variant.AvailableStock < 1)
            return BadRequest("Variante não encontrada.");

        var existing = cart.Items.FirstOrDefault(i => i.ProductVariantId == variantId);
        if (existing is not null)
        {
            if (existing.Quantity + quantity > variant.AvailableStock)
            {
                TempData["CartError"] = $"Só existem {variant.AvailableStock} unidades disponíveis.";
                return RedirectToAction(nameof(Index));
            }

            existing.Quantity += quantity;
            existing.UnitPrice = variant.Price;
        }
        else
        {
            if (quantity > variant.AvailableStock)
            {
                TempData["CartError"] = $"Só existem {variant.AvailableStock} unidades disponíveis.";
                return RedirectToAction(nameof(Index));
            }

            var cartItem = new CartItem
            {
                CartId           = cart.Id,
                ProductId        = variant.ProductId,
                ProductVariantId = variantId,
                Quantity         = quantity,
                UnitPrice        = variant.Price
            };
            cart.Items.Add(cartItem);
            _uow.Carts.AddItem(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Actualiza a quantidade de um item do carrinho.</summary>
    [HttpPost("/cart/update/{itemId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid itemId, [FromForm] int quantity, CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return NotFound();

        if (quantity < 1)
        {
            TempData["CartError"] = "A quantidade mínima é 1. Para remover o artigo, usa o botão de remoção.";
            return RedirectToAction(nameof(Index));
        }

        var variant = await _uow.Variants.GetByIdAsync(item.ProductVariantId, ct);
        if (variant is null || !variant.IsActive)
            return BadRequest("Variante não encontrada.");

        if (quantity > variant.AvailableStock)
        {
            TempData["CartError"] = $"Só existem {variant.AvailableStock} unidades disponíveis.";
            return RedirectToAction(nameof(Index));
        }

        item.Quantity = quantity;
        item.UnitPrice = variant.Price;
        cart.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Remove item do carrinho.</summary>
    [HttpPost("/cart/remove/{itemId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid itemId, CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<Cart> GetOrCreateCartAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Cart? cart = null;

        if (!string.IsNullOrEmpty(userId))
        {
            cart = await _uow.Carts.GetByUserIdAsync(userId, ct);
        }

        if (cart is null)
        {
            var token = Request.Cookies["cart_token"];
            if (!string.IsNullOrEmpty(token))
                cart = await _uow.Carts.GetByTokenAsync(token, ct);
        }

        if (cart is null)
        {
            cart = new Cart();
            await _uow.Carts.AddAsync(cart, ct);
            await _uow.SaveChangesAsync(ct);
            Response.Cookies.Append("cart_token", cart.CartToken,
                new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddDays(30) });
        }

        return cart;
    }
}
