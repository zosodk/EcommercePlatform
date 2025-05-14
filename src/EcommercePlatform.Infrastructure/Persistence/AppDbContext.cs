using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions; // For ToCollection, UseMongoDB etc.

namespace EcommercePlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; init; }
    public DbSet<ListingItem> Listings { get; init; }
    public DbSet<Order> Orders { get; init; }
    public DbSet<Review> Reviews { get; init; }
    // Note: Owned types like GeoLocation, ContactInfo, Address, OrderItemInfo, PaymentDetails
    // do not get their own DbSet properties. They are accessed via their owner entities.

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure collection names (MongoDB "tables")
        modelBuilder.Entity<User>().ToCollection("Users");
        modelBuilder.Entity<ListingItem>().ToCollection("Listings");
        modelBuilder.Entity<Order>().ToCollection("Orders");
        modelBuilder.Entity<Review>().ToCollection("Reviews");

        // Configure primary keys (EF Core convention is 'Id' or '<ClassName>Id').
        // The MongoDB provider typically maps a string 'Id' to MongoDB's '_id' (which can be an ObjectId).
        // Explicit configuration is usually not needed if conventions are followed.
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<ListingItem>().HasKey(l => l.Id);
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<Review>().HasKey(r => r.Id);


        // Configure Owned Entity Types for embedded documents:
        modelBuilder.Entity<User>(entity =>
        {
            entity.OwnsOne(u => u.ContactInformation, ci =>
            {
                ci.Property(p => p.PhoneNumber).HasElementName("phoneNumber"); // Explicit element naming
                ci.OwnsOne(c => c.ShippingAddress, sa => // Nested owned type
                {
                    sa.Property(a => a.Street).HasElementName("street");
                    sa.Property(a => a.City).HasElementName("city");
                    sa.Property(a => a.PostalCode).HasElementName("postalCode");
                    sa.Property(a => a.Country).HasElementName("country");
                });
            });
        });

        modelBuilder.Entity<ListingItem>(entity =>
        {
            entity.OwnsOne(l => l.Location, loc =>
            {
                loc.Property(g => g.Type).HasElementName("type");
                loc.Property(g => g.Coordinates).HasElementName("coordinates");
            });
            // ItemSpecifics (Dictionary<string, object>) is typically handled by default
            // by the MongoDB provider as an embedded BSON document.

            // OPTIMISTIC CONCURRENCY CONFIGURATION
            entity.Property(l => l.Version).IsConcurrencyToken(); // Use IsConcurrencyToken()
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.OwnsMany(o => o.Items, item =>
            {
                // Ensures Items is an embedded array.
                // Explicit element naming (if desired):
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
        });

        // Indexes:
        // Simple indexes can be defined here. The MongoDB EF Core provider has increasing support.
        // MongoDB EF Core Provider documentation for the latest on HasIndex capabilities.
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique(); // unique index with a custom name
        
        modelBuilder.Entity<ListingItem>().HasIndex(l => l.SellerId);
        modelBuilder.Entity<ListingItem>().HasIndex(l => l.Category);
        modelBuilder.Entity<ListingItem>().HasIndex(l => l.Status);
        
        // compound index (order matters)
        // Removed .HasName() as it was causing an error. MongoDB will auto-ge. a name.
        modelBuilder.Entity<ListingItem>()
            .HasIndex(l => new { l.Category, l.Status }); 


        modelBuilder.Entity<Order>().HasIndex(o => o.BuyerId);
        modelBuilder.Entity<Order>().HasIndex(o => o.SellerId);
        
        modelBuilder.Entity<Review>().HasIndex(r => r.SellerId);
        modelBuilder.Entity<Review>().HasIndex(r => r.ListingId);

        // For geospatial and text indexes, creating them directly in MongoDB shell
        // or via a script is often more reliable and offers more conf...
    }
}
