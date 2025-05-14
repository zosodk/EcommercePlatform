using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

    namespace EcommercePlatform.Infrastructure.Persistence;

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; init; }
        public DbSet<ListingItem> Listings { get; init; }
        public DbSet<Order> Orders { get; init; }
        public DbSet<Review> Reviews { get; init; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToCollection("Users");
            modelBuilder.Entity<ListingItem>().ToCollection("Listings");
            modelBuilder.Entity<Order>().ToCollection("Orders");
            modelBuilder.Entity<Review>().ToCollection("Reviews");

            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<ListingItem>().HasKey(l => l.Id);
            modelBuilder.Entity<Order>().HasKey(o => o.Id);
            modelBuilder.Entity<Review>().HasKey(r => r.Id);

            modelBuilder.Entity<User>(entity =>
            {
                entity.OwnsOne(u => u.ContactInformation, ci =>
                {
                    ci.Property(p => p.PhoneNumber).HasElementName("phoneNumber");
                    ci.OwnsOne(c => c.ShippingAddress, sa =>
                    {
                        sa.Property(a => a.Street).HasElementName("street");
                        sa.Property(a => a.City).HasElementName("city");
                        sa.Property(a => a.PostalCode).HasElementName("postalCode");
                        sa.Property(a => a.Country).HasElementName("country");
                    });
                });
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<ListingItem>(entity =>
            {
                entity.OwnsOne(l => l.Location, loc =>
                {
                    loc.Property(g => g.Type).HasElementName("type");
                    loc.Property(g => g.Coordinates).HasElementName("coordinates");
                });
                entity.Property(l => l.Version).IsConcurrencyToken();
                entity.HasIndex(l => l.SellerId);
                entity.HasIndex(l => l.Category);
                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => new { l.Category, l.Status }); // Compound index
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.OwnsMany(o => o.Items, item =>
                {
                    item.Property(i => i.ListingId).HasElementName("listingId");
                    item.Property(i => i.TitleSnapshot).HasElementName("titleSnapshot");
                    item.Property(i => i.PriceAtPurchase).HasElementName("priceAtPurchase");
                    item.Property(i => i.Quantity).HasElementName("quantity");
                });
                entity.OwnsOne(o => o.ShippingAddressSnapshot, sa =>
                {
                    sa.Property(a => a.Street).HasElementName("street");
                    sa.Property(a => a.City).HasElementName("city");
                    sa.Property(a => a.PostalCode).HasElementName("postalCode");
                    sa.Property(a => a.Country).HasElementName("country");
                });
                entity.OwnsOne(o => o.PaymentInfo, pi =>
                {
                    pi.Property(p => p.PaymentGatewayTransactionId).HasElementName("paymentGatewayTransactionId");
                    pi.Property(p => p.PaymentMethod).HasElementName("paymentMethod");
                    pi.Property(p => p.PaymentStatus).HasElementName("paymentStatus");
                    pi.Property(p => p.PaidAt).HasElementName("paidAt");
                });
                entity.HasIndex(o => o.BuyerId);
                entity.HasIndex(o => o.SellerId);
            });
             modelBuilder.Entity<Review>(entity =>
            {
                entity.HasIndex(r => r.SellerId);
                entity.HasIndex(r => r.ListingId);
            });
        }
    }