using EcommercePlatform.Application.Interfaces.Repositories;
using EcommercePlatform.Application.Interfaces.Services;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Infrastructure.Persistence;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore; 

namespace EcommercePlatform.Application.Orders
{
    // PlaceOrderCommand DTO from previous example is still valid
    public class PlaceOrderCommand
    {
        public string BuyerId { get; set; } = string.Empty;
        public string ListingId { get; set; } = string.Empty;
        // ... other order details
    }

    public interface IUnitOfWork : IDisposable // A common pattern with EF Core
    {
        IListingRepository Listings { get; }
        IOrderRepository Orders { get; }
        // ... other repositories
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default); // If explicit transaction control needed
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }


    public class OrderService
    {
        private readonly IListingRepository _listingRepository;
        private readonly IOrderRepository _orderRepository; // Assuming this interface and its EF Core impl exist
        private readonly ICacheService _cacheService;
        // If using EF Core's implicit transaction per SaveChangesAsync, you might not need to manage IClientSessionHandle here.
        // If you need explicit control over a transaction spanning multiple operations or repositories,
        // you'd inject the DbContext or a UnitOfWork that manages the DbContext and its session.
        // The MongoDB EF Core provider can use an IClientSessionHandle.
        private readonly AppDbContext _dbContext; // Injecting DbContext for transaction control

        public OrderService(
            IListingRepository listingRepository,
            IOrderRepository orderRepository,
            ICacheService cacheService,
            AppDbContext dbContext) // Inject AppDbContext from Infrastructure
        {
            _listingRepository = listingRepository;
            _orderRepository = orderRepository;
            _cacheService = cacheService;
            _dbContext = dbContext;
        }

        public async Task<Order?> PlaceOrderAsync(PlaceOrderCommand command)
        {
            // Start a transaction using the DbContext.
            // The MongoDB EF Core provider should use the underlying IClientSessionHandle.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var listing = await _listingRepository.GetByIdAndStatusAsync(command.ListingId, "available");
                if (listing == null)
                {
                    throw new InvalidOperationException("Listing not available or not found.");
                }

                listing.Status = "sold";
                listing.UpdatedAt = DateTime.UtcNow;
                // Version for optimistic concurrency will be checked by EF Core during SaveChanges if configured.
                var updateSuccess = await _listingRepository.UpdateAsync(listing); // This marks entity as Modified
                if(!updateSuccess) // The repository's UpdateAsync might return bool based on SaveChanges or other logic
                {
                     // Or rely on SaveChanges throwing DbUpdateConcurrencyException
                    throw new InvalidOperationException("Failed to update listing status, possibly due to concurrency.");
                }


                var order = new Order
                {
                    BuyerId = command.BuyerId,
                    SellerId = listing.SellerId,
                    Items = new List<OrderItemInfo>
                    {
                        new OrderItemInfo
                        {
                            ListingId = listing.Id!,
                            Title = listing.Title,
                            PriceAtPurchase = listing.Price,
                            Quantity = 1
                        }
                    },
                    TotalAmount = listing.Price,
                    Currency = listing.Currency,
                    OrderStatus = "pending_payment",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _orderRepository.CreateAsync(order); // This marks entity as Added

                // All changes are committed to the database in one go.
                // If UpdateAsync and CreateAsync don't call SaveChangesAsync internally (they shouldn't in a UoW pattern),
                // then a single SaveChangesAsync here will commit all modifications within the transaction.
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                await _cacheService.RemoveAsync($"listing:{command.ListingId}");

                return order;
            }
            catch (DbUpdateConcurrencyException ex) // Specific EF Core exception for optimistic concurrency
            {
                await transaction.RollbackAsync();
                // Log ex
                throw new InvalidOperationException("Failed to place order due to a concurrency conflict. Please try again.", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log ex
                throw; // Re-throw to be handled by global error handling
            }
        }
    }
}