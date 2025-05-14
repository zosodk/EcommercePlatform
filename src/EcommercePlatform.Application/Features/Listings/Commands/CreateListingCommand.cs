using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcommercePlatform.Application.Features.Listings.Commands;
public class CreateListingCommand
{
    [Required] public string SellerId { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(5000)] public string Description { get; set; } = string.Empty;
    [Required] public string Category { get; set; } = string.Empty;
    [Range(0.01, 1000000.00)] public decimal Price { get; set; }
    [Required] public string Currency { get; set; } = "USD";
    [Required] public string Condition { get; set; } = string.Empty;
    public Dictionary<string, object>? ItemSpecifics { get; set; }
    public List<string>? ImageObjectKeys { get; set; }
    public double? LocationLongitude { get; set; }
    public double? LocationLatitude { get; set; }
    public List<string>? Tags { get; set; }
}