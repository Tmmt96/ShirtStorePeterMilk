using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Entities;
using ShirtStore.Infrastructure.Data;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// ── JSON ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AuthorizeFolder("/Account");
    });

builder.Services.AddRazorPages();

// ── EF Core ────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=(localdb)\\mssqllocaldb;Database=ShirtStore;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

// ── Identity ───────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// ── Stripe ─────────────────────────────────────────────────────────────────
var stripeKey = string.IsNullOrWhiteSpace(builder.Configuration["Stripe:SecretKey"])
                ? (builder.Environment.IsDevelopment()
                   ? "sk_test_placeholder"
                   : throw new InvalidOperationException("Stripe:SecretKey não configurado."))
                : builder.Configuration["Stripe:SecretKey"]!;
StripeConfiguration.ApiKey = stripeKey;
builder.Services.AddSingleton(new StripeClient(StripeConfiguration.ApiKey));

// ── Email (abstração) ──────────────────────────────────────────────────────
// TODO: registar implementação concreta (Brevo / Resend / SMTP) na fase de email.
builder.Services.AddScoped<ShirtStore.Domain.Interfaces.IEmailSender, ShirtStore.Infrastructure.Email.NoOpEmailSender>();

// ── Repositórios / Unit of Work ────────────────────────────────────────────
builder.Services.AddScoped<ShirtStore.Domain.Interfaces.IProductRepository, ShirtStore.Infrastructure.Data.ProductRepository>();
builder.Services.AddScoped<ShirtStore.Domain.Interfaces.IProductVariantRepository, ShirtStore.Infrastructure.Data.ProductVariantRepository>();
builder.Services.AddScoped<ShirtStore.Domain.Interfaces.ICartRepository, ShirtStore.Infrastructure.Data.CartRepository>();
builder.Services.AddScoped<ShirtStore.Domain.Interfaces.IOrderRepository, ShirtStore.Infrastructure.Data.OrderRepository>();
builder.Services.AddScoped<ShirtStore.Domain.Interfaces.IUnitOfWork, ShirtStore.Infrastructure.Data.UnitOfWork>();

// ── Pipeline HTTP ──────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate em Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SeedCatalogAsync(db);
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// robots.txt dinâmico
app.MapGet("/robots.txt", async context =>
{
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("""
        User-agent: *
        Allow: /
        Disallow: /Account/
        Disallow: /Cart/
        Disallow: /Checkout/
        Sitemap: {sitemapUrl}
        """.Replace("{sitemapUrl}", $"{context.Request.Scheme}://{context.Request.Host}/sitemap.xml"));
});

// sitemap.xml dinâmico
app.MapGet("/sitemap.xml", async context =>
{
    var db = context.RequestServices.GetRequiredService<AppDbContext>();
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var products = await db.Products
        .Where(p => p.Published && !p.NoIndex)
        .ToListAsync();

    var xml = new System.Text.StringBuilder();
    xml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
    xml.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

    // Páginas estáticas
    var staticPages = new[] { "/", "/catalog", "/about", "/terms", "/privacy" };
    foreach (var page in staticPages)
    {
        xml.AppendLine($"  <url><loc>{baseUrl}{page}</loc><changefreq>weekly</changefreq><priority>0.8</priority></url>");
    }

    // Produtos
    foreach (var p in products)
    {
        var lastMod = p.UpdatedAt.ToString("yyyy-MM-dd");
        xml.AppendLine($"  <url><loc>{baseUrl}/product/{p.Slug}</loc><lastmod>{lastMod}</lastmod><changefreq>weekly</changefreq><priority>0.9</priority></url>");
    }

    xml.AppendLine("</urlset>");
    context.Response.ContentType = "application/xml";
    await context.Response.WriteAsync(xml.ToString());
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();

static async Task SeedCatalogAsync(AppDbContext db)
{
    if (await db.Products.AnyAsync())
        return;

    var catalog = new[]
    {
        new { Name = "KTM — Ka Tombo Moço", Slug = "ktm-ka-tombo-moco", Image = "catalog-page-01.png", Description = "Uma camisola preta para quem leva a estrada a sério.", Tags = "motor,portugal,preto" },
        new { Name = "Avia — Um Pessegueiro na Ilha", Slug = "avia-pessegueiro-na-ilha", Image = "catalog-page-02.png", Description = "Uma imagem forte, direta e feita para durar.", Tags = "avia,frase,preto" },
        new { Name = "De Ser", Slug = "de-ser", Image = "catalog-page-03.png", Description = "O essencial também pode ter personalidade.", Tags = "bar,frase,preto" },
        new { Name = "Comigo Hoje", Slug = "comigo-hoje", Image = "catalog-page-04.png", Description = "Para os dias em que só precisas de uma boa desculpa.", Tags = "humor,frase,preto" },
        new { Name = "Faz-te Bem", Slug = "faz-te-bem", Image = "catalog-page-05.png", Description = "Uma pequena mensagem com uma grande atitude.", Tags = "frase,design,preto" },
        new { Name = "Faz-te Bem — Play", Slug = "faz-te-bem-play", Image = "catalog-page-06.png", Description = "A mesma energia, com banda sonora incluída.", Tags = "gaming,frase,preto" },
        new { Name = "Gente Boa Neste Mundo", Slug = "gente-boa-neste-mundo", Image = "catalog-page-07.png", Description = "Uma homenagem simples às pessoas certas.", Tags = "indesit,frase,preto" },
        new { Name = "Naquele", Slug = "naquele", Image = "catalog-page-08.png", Description = "Minimalismo com uma assinatura desportiva.", Tags = "streetwear,preto" },
        new { Name = "Molhei-te Mas Depois...", Slug = "molhei-te-mas-depois", Image = "catalog-page-09.png", Description = "Humor português para usar sem cerimónia.", Tags = "skate,humor,preto" }
    };

    foreach (var item in catalog)
    {
        var product = new ShirtStore.Domain.Entities.Product
        {
            Name = item.Name,
            Slug = item.Slug,
            Description = item.Description,
            ImageUrl = $"/images/catalog/{item.Image}",
            Category = "Coleção Peter Milk",
            Tags = item.Tags,
            SeoTitle = $"{item.Name} | ShirtStore",
            SeoDescription = item.Description,
            BasePrice = 19.90m,
            Currency = "EUR",
            Published = true
        };

        foreach (var size in new[] { "S", "M", "L", "XL" })
        {
            product.Variants.Add(new ProductVariant
            {
                Product = product,
                ProductId = product.Id,
                Sku = $"{item.Slug.ToUpperInvariant()}-{size}",
                Size = size,
                Color = "Preto",
                ColorHex = "#171717",
                Price = 19.90m,
                Stock = 12
            });
        }

        db.Products.Add(product);
    }

    await db.SaveChangesAsync();
}
