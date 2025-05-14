using System;
using System.Collections.Generic;

namespace EcommercePlatform.Domain.Entities;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Or MongoDB ObjectId string
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Should be unique
    public string HashedPassword { get; set; } = string.Empty; // Store hashed passwords only
    public string? ProfileImageUrl { get; set; }
    public ContactInfo? ContactInformation { get; set; } // Owned type
    public decimal AverageRating { get; set; } = 0; // Denormalized, updated by reviews
    public int TotalReviews { get; set; } = 0;     // Denormalized
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (conceptual for EF Core with NoSQL, not enforced by DB constraints)
    // public virtual ICollection<ListingItem> Listings { get; set; } = new List<ListingItem>();
    // public virtual ICollection<Order> OrdersPlaced { get; set; } = new List<Order>();
    // public virtual ICollection<Order> OrdersReceived { get; set; } = new List<Order>();
    // public virtual ICollection<Review> ReviewsWritten { get; set; } = new List<Review>();
    // public virtual ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
}

public class ContactInfo // Owned type for User
{
    public string? PhoneNumber { get; set; }
    public Address? ShippingAddress { get; set; } // Can be another owned type
}

public class Address // Owned type
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}