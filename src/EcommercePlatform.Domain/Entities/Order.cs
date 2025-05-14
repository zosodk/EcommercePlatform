using System;
    using System.Collections.Generic;

    namespace EcommercePlatform.Domain.Entities;

    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Or MongoDB ObjectId string
        public string BuyerId { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty; // Denormalized for easier querying if needed, or just from items
        public List<OrderItemInfo> Items { get; set; } = new List<OrderItemInfo>(); // Owned collection
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string OrderStatus { get; set; } = string.Empty; // E.g., "PendingPayment", "Paid", "Shipped", "Delivered", "Cancelled"
        public Address? ShippingAddressSnapshot { get; set; } // Snapshot of shipping address, owned type
        public PaymentDetails? PaymentInfo { get; set; } // Owned type
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (conceptual)
        // public virtual User Buyer { get; set; }
        // public virtual User Seller { get; set; } // If SellerId is a direct For.K
    }

    public static class OrderStatuses //  statuses
    {
        public const string PendingPayment = "PendingPayment";
        public const string Paid = "Paid";
        public const string AwaitingShipment = "AwaitingShipment";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed"; // After delivery and no issues
        public const string PaymentFailed = "PaymentFailed";
    }

    public class PaymentDetails // Owned type
    {
        public string PaymentGatewayTransactionId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // e.g., "CreditCard", "PayPal"
        public string PaymentStatus { get; set; } = string.Empty; // e.g., "Succeeded", "Failed", "Pending"
        public DateTime? PaidAt { get; set; }
    }