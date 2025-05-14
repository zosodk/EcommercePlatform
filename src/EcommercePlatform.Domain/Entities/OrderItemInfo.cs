using System;

namespace EcommercePlatform.Domain.Entities;

public class OrderItemInfo // Owned type for Order
{
    public string ListingId { get; set; } = string.Empty;
    public string TitleSnapshot { get; set; } = string.Empty; // Snapshot of title at time of order
    public decimal PriceAtPurchase { get; set; }
    public int Quantity { get; set; }

    // Private constructor for EF Core if needed, or ensure properties are settable
    private OrderItemInfo() {}

    public static OrderItemInfo Create(string listingId, string titleSnapshot, decimal priceAtPurchase, int quantity)
    {
        if (string.IsNullOrWhiteSpace(listingId))
            throw new ArgumentException("Listing ID cannot be empty.", nameof(listingId));
        if (string.IsNullOrWhiteSpace(titleSnapshot))
            throw new ArgumentException("Title snapshot cannot be empty.", nameof(titleSnapshot));
        if (priceAtPurchase < 0)
            throw new ArgumentOutOfRangeException(nameof(priceAtPurchase), "Price cannot be negative.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        return new OrderItemInfo
        {
            ListingId = listingId,
            TitleSnapshot = titleSnapshot,
            PriceAtPurchase = priceAtPurchase,
            Quantity = quantity
        };
    }
}