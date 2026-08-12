using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;
using ShirtStore.Domain.Enums;
using ShirtStore.Web.Models;

namespace ShirtStore.Web.Controllers;

/// <summary>
/// Checkout — cria sessão Stripe Checkout e processa webhooks.
/// </summary>
public class CheckoutController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _email;
    private readonly IConfiguration _configuration;

    public CheckoutController(IUnitOfWork uow, IEmailSender email, IConfiguration configuration)
    {
        _uow           = uow;
        _email         = email;
        _configuration = configuration;
    }

    /// <summary>Apresenta o checkout de convidado.</summary>
    [HttpGet("/checkout")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await GetCurrentCartAsync(ct);
        if (cart is null || !cart.Items.Any())
            return RedirectToAction(nameof(CartController.Index), "Cart");

        var model = CreateCheckoutModel(cart);
        model.CustomerName = User.Identity?.IsAuthenticated == true
            ? $"{User.FindFirst("given_name")?.Value} {User.FindFirst("family_name")?.Value}".Trim()
            : string.Empty;
        model.CustomerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        return View(model);
    }

    /// <summary>Valida os dados do checkout, cria a encomenda e redireciona para o pagamento.</summary>
    [HttpPost("/checkout/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(CheckoutViewModel model, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var cart = await GetCurrentCartAsync(ct);

        if (cart is null || !cart.Items.Any())
            return RedirectToAction(nameof(CartController.Index), "Cart");

        model.Cart = cart;
        if (!ModelState.IsValid)
        {
            PopulateSummary(model, cart);
            return View("Index", model);
        }

        if (string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]))
        {
            ModelState.AddModelError(string.Empty, "O pagamento está temporariamente indisponível enquanto o checkout não está configurado.");
            PopulateSummary(model, cart);
            return View("Index", model);
        }

        var checkoutItems = new List<(CartItem Item, ProductVariant Variant)>();
        foreach (var item in cart.Items)
        {
            var variant = await _uow.Variants.GetByIdAsync(item.ProductVariantId, ct);
            if (variant is null || !variant.IsActive || item.Quantity < 1 || item.Quantity > variant.AvailableStock)
            {
                ModelState.AddModelError(string.Empty, $"A variante de {item.Product.Name} já não tem stock suficiente.");
                PopulateSummary(model, cart);
                return View("Index", model);
            }

            checkoutItems.Add((item, variant));
        }

        // ── Calcular totais no servidor (nunca confiar no browser) ───────────
        decimal subTotal = checkoutItems.Sum(x => x.Variant.Price * x.Item.Quantity);
        decimal shipping = subTotal >= 50 ? 0 : 4.99m;   // portes grátis acima de €50
        decimal tax      = 0m;                            // TODO: calcular IVA por país
        decimal total    = subTotal + shipping + tax;

        // ── Criar encomenda pendente ─────────────────────────────────────────
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant();
        var order = new Order
        {
            OrderNumber   = orderNumber,
            UserId        = userId,
            CustomerEmail = model.CustomerEmail.Trim(),
            CustomerName  = model.CustomerName.Trim(),
            CustomerPhone = model.CustomerPhone.Trim(),
            BillingAddressLine1 = model.ShippingAddressLine1.Trim(),
            BillingAddressLine2 = model.ShippingAddressLine2?.Trim(),
            BillingCity = model.ShippingCity.Trim(),
            BillingPostalCode = model.ShippingPostalCode.Trim(),
            BillingCountry = model.ShippingCountry.Trim(),
            BillingTaxId = model.TaxId?.Trim(),
            ShippingAddressLine1 = model.ShippingAddressLine1.Trim(),
            ShippingAddressLine2 = model.ShippingAddressLine2?.Trim(),
            ShippingCity = model.ShippingCity.Trim(),
            ShippingPostalCode = model.ShippingPostalCode.Trim(),
            ShippingCountry = model.ShippingCountry.Trim(),
            SubTotal      = subTotal,
            Shipping      = shipping,
            Tax           = tax,
            Total         = total,
            Currency      = "EUR",
            Status        = OrderStatus.PendingPayment
        };

        foreach (var checkoutItem in checkoutItems)
        {
            var item = checkoutItem.Item;
            var variant = checkoutItem.Variant;

            order.Items.Add(new OrderItem
            {
                ProductId       = item.ProductId,
                ProductName     = item.Product.Name,
                ProductVariantId= item.ProductVariantId,
                VariantSku      = variant.Sku,
                VariantSize     = variant.Size,
                VariantColor    = variant.Color,
                Quantity        = item.Quantity,
                UnitPrice        = variant.Price
            });

            // Reservar stock
            variant.Reserved += item.Quantity;
            _uow.Variants.Update(variant);
        }

        await _uow.Orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        // ── Criar sessão Stripe Checkout ─────────────────────────────────────
        var lineItems = order.Items.Select(oi => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "eur",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = $"{oi.ProductName} — {oi.VariantSize} / {oi.VariantColor}"
                },
                UnitAmount = (long)(oi.UnitPrice * 100)
            },
            Quantity = (long)oi.Quantity
        }).ToList();

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems          = lineItems,
            Mode               = "payment",
            SuccessUrl         = $"{Request.Scheme}://{Request.Host}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl          = $"{Request.Scheme}://{Request.Host}/checkout/cancel",
            CustomerEmail      = order.CustomerEmail,
            Metadata           = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        order.StripeCheckoutSessionId = session.Id;
        await _uow.SaveChangesAsync(ct);

        return Redirect(session.Url!);
    }

    private async Task<Cart?> GetCurrentCartAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var userCart = await _uow.Carts.GetByUserIdAsync(userId, ct);
            if (userCart is not null)
                return userCart;
        }

        var token = Request.Cookies["cart_token"];
        return string.IsNullOrEmpty(token) ? null : await _uow.Carts.GetByTokenAsync(token, ct);
    }

    private static CheckoutViewModel CreateCheckoutModel(Cart cart)
    {
        var model = new CheckoutViewModel();
        PopulateSummary(model, cart);
        return model;
    }

    private static void PopulateSummary(CheckoutViewModel model, Cart cart)
    {
        model.Cart = cart;
        model.SubTotal = cart.Items.Sum(item => item.UnitPrice * item.Quantity);
        model.Shipping = model.SubTotal >= 50 ? 0 : 4.99m;
        model.Total = model.SubTotal + model.Shipping;
    }

    /// <summary>Página de sucesso — aguarda confirmação do webhook.</summary>
    [HttpGet("/checkout/success")]
    public async Task<IActionResult> Success([FromQuery] string? session_id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(session_id))
            return RedirectToAction(nameof(CartController.Index), "Cart");

        var service = new SessionService();
        var session = await service.GetAsync(session_id, cancellationToken: ct);

        var order = await _uow.Orders.GetByCheckoutSessionIdAsync(session_id, ct);

        ViewData["Title"] = "Obrigado pela sua encomenda!";
        ViewData["NoIndex"] = true;
        return View(order);
    }

    /// <summary>Página de cancelamento.</summary>
    [HttpGet("/checkout/cancel")]
    public IActionResult Cancel()
    {
        ViewData["Title"] = "Checkout cancelado";
        ViewData["NoIndex"] = true;
        return View();
    }

    /// <summary>Webhook Stripe — valida assinatura e atualiza estado da encomenda.</summary>
    [HttpPost("/webhooks/stripe")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var stripeEvent = EventUtility.ConstructEvent(json,
            Request.Headers["Stripe-Signature"]!,
            HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Stripe:WebhookSecret"]
                ?? throw new InvalidOperationException("Stripe:WebhookSecret não configurado."));

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompleted(stripeEvent.Data.Object as Session, ct);
                break;
            case "payment_intent.payment_failed":
                await HandlePaymentFailed(stripeEvent.Data.Object as PaymentIntent, ct);
                break;
        }

        return Ok();
    }

    private async Task HandleCheckoutCompleted(Session? session, CancellationToken ct)
    {
        if (session is null) return;

        var order = await _uow.Orders.GetByCheckoutSessionIdAsync(session.Id, ct);

        if (order is null) return;
        if (order.Status == OrderStatus.Paid) return;

        order.Status = OrderStatus.Paid;
        order.StripePaymentIntentId = session.PaymentIntentId;
        await _uow.SaveChangesAsync(ct);

        // Enviar email de confirmação (assíncrono, não bloquear o webhook)
        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendAsync(
                    order.CustomerEmail,
                    $"Confirmação de encomenda {order.OrderNumber}",
                    $"<p>Obrigado pela sua encomenda <strong>{order.OrderNumber}</strong>!</p>" +
                    $"<p>Total: <strong>€{order.Total:F2}</strong></p>",
                    CancellationToken.None);
            }
            catch { /* log + retry */ }
        }, CancellationToken.None);
    }

    private async Task HandlePaymentFailed(PaymentIntent? intent, CancellationToken ct)
    {
        if (intent is null) return;

        var order = await _uow.Orders.GetByPaymentIntentIdAsync(intent.Id, ct);

        if (order is null) return;

        order.Status = OrderStatus.PaymentFailed;
        await _uow.SaveChangesAsync(ct);
    }
}
