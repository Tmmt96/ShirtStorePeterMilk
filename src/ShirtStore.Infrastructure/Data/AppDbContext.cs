using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product>       Products       => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Cart>          Carts          => Set<Cart>();
    public DbSet<CartItem>      CartItems      => Set<CartItem>();
    public DbSet<Order>         Orders         => Set<Order>();
    public DbSet<OrderItem>     OrderItems     => Set<OrderItem>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Product ──────────────────────────────────────────────────────────
        builder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.Published);
            e.Property(p => p.BasePrice).HasPrecision(18, 2);
        });

        // ── ProductVariant ───────────────────────────────────────────────────
        builder.Entity<ProductVariant>(e =>
        {
            e.HasIndex(v => v.Sku).IsUnique();
            e.HasIndex(v => new { v.ProductId, v.Size, v.Color }).IsUnique();
            e.Property(v => v.Price).HasPrecision(18, 2);
            e.HasOne(v => v.Product)
             .WithMany(p => p.Variants)
             .HasForeignKey(v => v.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Cart ─────────────────────────────────────────────────────────────
        builder.Entity<Cart>(e =>
        {
            e.HasIndex(c => c.CartToken).IsUnique();
            e.HasIndex(c => c.UserId);
        });

        // ── CartItem ─────────────────────────────────────────────────────────
        builder.Entity<CartItem>(e =>
        {
            e.HasIndex(ci => new { ci.CartId, ci.ProductVariantId }).IsUnique();
            e.Property(ci => ci.UnitPrice).HasPrecision(18, 2);
            e.HasOne(ci => ci.Cart)
             .WithMany(c => c.Items)
             .HasForeignKey(ci => ci.CartId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ci => ci.Product)
             .WithMany()
             .HasForeignKey(ci => ci.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ci => ci.Variant)
             .WithMany()
             .HasForeignKey(ci => ci.ProductVariantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Order ────────────────────────────────────────────────────────────
        builder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.UserId);
            e.HasIndex(o => o.Status);
            e.Property(o => o.SubTotal).HasPrecision(18, 2);
            e.Property(o => o.Shipping).HasPrecision(18, 2);
            e.Property(o => o.Tax).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
        });

        // ── OrderItem ────────────────────────────────────────────────────────
        builder.Entity<OrderItem>(e =>
        {
            e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            e.HasOne(oi => oi.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PaymentAttempt ───────────────────────────────────────────────────
        builder.Entity<PaymentAttempt>(e =>
        {
            e.HasIndex(p => p.StripePaymentIntentId).IsUnique();
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.HasOne(p => p.Order)
             .WithMany(o => o.PaymentAttempts)
             .HasForeignKey(p => p.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
