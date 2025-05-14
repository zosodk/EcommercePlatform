// This resolves the circular dependency.

using EcommercePlatform.Application.Features.Orders.Commands; // Now correctly references the command DTO
using EcommercePlatform.Application.Interfaces.Repositories;
using EcommercePlatform.Application.Interfaces.Services; // For ICacheService
using EcommercePlatform.Application.Interfaces.Common; // For IUnitOfWork
using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore; // For DbUpdateConcurrencyException and IDbContextTransaction
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // For List
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace EcommercePlatform.Application.Features.Orders.Handlers;

public class PlaceOrderCommandHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IListingRepository listingRepository,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var buyer = await _userRepository.GetByIdAsync(command.BuyerId);
        if (buyer == null)
        {
            _logger.LogWarning("PlaceOrder: Buyer with ID {BuyerId} not found.", command.BuyerId);
            throw new ApplicationException("Buyer not found.");
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            _logger.LogInformation("PlaceOrder: Transaction started for ListingId {ListingId} by BuyerId {BuyerId}.", command.ListingId, command.BuyerId);

            var listing = await _listingRepository.GetByIdAsync(command.ListingId);
            if (listing == null)
            {
                _logger.LogWarning("PlaceOrder: Listing with ID {ListingId} not found.", command.ListingId);
                throw new ApplicationException("Listing not found.");
            }

            if (listing.Status != ListingStatus.Available)
            {
                _logger.LogWarning("PlaceOrder: Listing {ListingId} is not available. Current status: {Status}", command.ListingId, listing.Status);
                throw new ApplicationException($"Listing is no longer available (Status: {listing.Status}).");
            }
            
            if (listing.SellerId == command.BuyerId)
            {
                _logger.LogWarning("PlaceOrder: Buyer {BuyerId} cannot purchase their own listing {ListingId}.", command.BuyerId, command.ListingId);
                throw new ApplicationException("You cannot purchase your own listing.");
            }

            listing.Status = ListingStatus.Sold;
            listing.UpdatedAt = DateTime.UtcNow;
            await _listingRepository.UpdateAsync(listing);

            var order = new Order
            {
                BuyerId = command.BuyerId,
                SellerId = listing.SellerId,
                Items = new List<OrderItemInfo>
                {
                    OrderItemInfo.Create(
                        listingId: listing.Id,
                        titleSnapshot: listing.Title,
                        priceAtPurchase: listing.Price,
                        quantity: command.Quantity
                    )
                },
                TotalAmount = listing.Price * command.Quantity,
                Currency = listing.Currency,
                OrderStatus = OrderStatuses.PendingPayment,
                ShippingAddressSnapshot = new Address
                {
                    Street = command.ShippingAddress.Street,
                    City = command.ShippingAddress.City,
                    PostalCode = command.ShippingAddress.PostalCode,
                    Country = command.ShippingAddress.Country
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _orderRepository.CreateAsync(order);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            _logger.LogInformation("PlaceOrder: Transaction committed for OrderId {OrderId} (ListingId {ListingId}).", order.Id, command.ListingId);

            try
            {
                await _cacheService.RemoveAsync($"listing:{listing.Id}");
                _logger.LogInformation("PlaceOrder: Cache invalidated for ListingId {ListingId}.", listing.Id);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "PlaceOrder: Failed to invalidate cache for ListingId {ListingId} after successful order. This is non-critical.", listing.Id);
            }
            
            return order.Id;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "PlaceOrder: Concurrency conflict for ListingId {ListingId}.", command.ListingId);
            if (transaction != null) await _unitOfWork.RollbackTransactionAsync(transaction, CancellationToken.None);
            throw new ApplicationException("The item was just sold or updated by someone else. Please try again.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PlaceOrder: Exception occurred for ListingId {ListingId}. Rolling back transaction.", command.ListingId);
            if (transaction != null)
            {
               try
               {
                   await _unitOfWork.RollbackTransactionAsync(transaction, CancellationToken.None);
                   _logger.LogInformation("PlaceOrder: Transaction rolled back for ListingId {ListingId}.", command.ListingId);
               }
               catch (Exception rbEx)
               {
                   _logger.LogError(rbEx, "PlaceOrder: Exception during transaction rollback for ListingId {ListingId}.", command.ListingId);
               }
            }
            if (ex is ApplicationException) throw;
            throw new ApplicationException("An error occurred while placing the order. Please try again.", ex);
        }
    }
}