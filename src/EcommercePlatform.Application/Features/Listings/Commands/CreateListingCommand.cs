using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // For basic validation attributes
// using MediatR;
//
// // If I decide to use MediatR, this command would implement IRequest<ListingItemDto or string>

namespace EcommercePlatform.Application.Features.Listings.Commands;

// public class CreateListingCommand : IRequest<string> // Example with MediatR returning listing ID
public class CreateListingCommand // Simple DTO for now
{
    [Required]
    public string SellerId { get; set; } = string.Empty; // Should be set from authenticated user context

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Range(0.01, 1000000.00)]
    public decimal Price { get; set; }

    [Required]
    public string Currency { get; set; } = "USD";

    [Required]
    public string Condition { get; set; } = string.Empty;

    public Dictionary<string, object>? ItemSpecifics { get; set; }

    // Client will first get presigned URLs, upload files, then send object keys here.
    public List<string>? ImageObjectKeys { get; set; }

    public double? LocationLongitude { get; set; }
    public double? LocationLatitude { get; set; }
    public List<string>? Tags { get; set; }
}