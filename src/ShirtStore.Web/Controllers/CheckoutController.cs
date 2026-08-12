using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using ShirtStore.Domain.Interfaces;
using ShirtStore.Domain.Entities;
using ShirtStore.Domain.Enums;

namespace ShirtStore.Web.Controllers;

/// <summary>
/// Checkout — cria sessão Stripe Checkout e processa webhooks.
/// </summary>
public class CheckoutController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _email;
    public CheckoutController(IUnitOfWork uow, IEmailSender email)
    {
        _uow   = uow;
        _email = email;
    }

    /// <summary>Inicia checkout — cria sessão Stripe e redireciona.</summary>
    [HttpPost("/checkout/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Cart? cart = null;

        if (!string.IsNullOrEmpty(userId))
            cart = await _uow.Carts.GetByUserIdAsync(userId, ct);

        if (cart is null)
        {
            var token = Request.Cookies["cart_token"];
            if (!string.IsNullOrEmpty(token))
                cart = await _uow.Carts.GetByTokenAsync(token, ct);
        }

        if (cart is null || !cart.Items.Any())
            return RedirectToAction(nameof(CartController.Index), "Cart");

        // ── Calcular totais no servidor (nunca confiar no browser) ───────────
        decimal subTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        decimal shipping = subTotal >= 50 ? 0 : 4.99m;   // portes grátis acima de €50
        decimal tax      = 0m;                            // TODO: calcular IVA por país
        decimal total    = subTotal + shipping + tax;

        // ── Criar encomenda pendente ─────────────────────────────────────────
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 16).ToUpper();
        var order = new Order
        {
            OrderNumber   = orderNumber,
            UserId        = userId,
            CustomerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
            CustomerName  = $"{User.FindFirst("given_name")?.Value} {User.FindFirst("family_name")?.Value}".Trim(),
            SubTotal      = subTotal,
            Shipping      = shipping,
            Tax           = tax,
            Total         = total,
            Currency      = "EUR",
            Status        = OrderStatus.PendingPayment
        };

        foreach (var item in cart.Items)
        {
            var variant = await _uow.Variants.GetByIdAsync(item.ProductVariantId, ct);
            if (variant is null) continue;

            order.Items.Add(new OrderItem
            {
                ProductId       = item.ProductId,
                ProductName     = item.Product.Name,
                ProductVariantId= item.ProductVariantId,
                VariantSku      = variant.Sku,
                VariantSize     = variant.Size,
                VariantColor    = variant.Color,
                Quantity        = item.Quantity,
                UnitPrice       = item.UnitPrice
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
