using System;
using System.Collections.Generic;

namespace EcommercePlatform.Domain.Entities;

public class ListingItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // MongoDB ObjectId string
    public string SellerId { get; set; } = string.Empty; // Foreign key to User.Id
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Could be an enum or a lookup table/collection
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD"; // Could be an enum
    public string Condition { get; set; } = string.Empty; // E.g., "New", "Used - Like New", "Used - Good"
    public Dictionary<string, object> ItemSpecifics { get; set; } = new Dictionary<string, object>(); // Flexible attributes
    public List<string> ImageUrls { get; set; } = new List<string>(); // URLs to images in cloud storage
    public string Status { get; set; } = ListingStatus.Available; // E.g., "Available", "Reserved", "Sold", "Delisted"
    public List<string>? Tags { get; set; }
    public GeoLocation? Location { get; set; } // Owned type
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1; // For optimistic concurrency

    // Navigation property (conceptual)
    // public virtual User Seller { get; set; }
}

public static class ListingStatus // Using a static class for constants for simplicity
{
    public const string Available = "Available";
    public const string Reserved = "Reserved";
    public const string Sold = "Sold";
    public const string Delisted = "Delisted";
}