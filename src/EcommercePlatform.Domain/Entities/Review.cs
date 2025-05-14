using System;

namespace EcommercePlatform.Domain.Entities;

public class Review
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Or MongoDB ObjectId string
    public string ReviewerId { get; set; } = string.Empty; // User who wrote the review
    public string SellerId { get; set; } = string.Empty;   // User being reviewed
    public string OrderId { get; set; } = string.Empty;    // Link review to a specific transaction
    public string? ListingId { get; set; }                  // Optional: If review is item-specific
    public int Rating { get; set; }                       // Like: 1 to 5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (conceptual)
    // public virtual User Reviewer { get; set; }
    // public virtual User Seller { get; set; }
    // public virtual Order AssociatedOrder { get; set; }
}