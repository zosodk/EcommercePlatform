using System.ComponentModel.DataAnnotations;

namespace EcommercePlatform.Application.Features.Orders.Commands;

public class PlaceOrderCommand
{
    [Required]
    public string BuyerId { get; set; } = string.Empty;

    [Required]
    public string ListingId { get; set; } = string.Empty;

    [Required]
    [Range(1, 100)] // Example range, adjust as necessary
    public int Quantity { get; set; } = 1;

    [Required]
    public ShippingAddressCommandDto ShippingAddress { get; set; } = new();
}

public class ShippingAddressCommandDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Country { get; set; } = string.Empty;
}