using EcommercePlatform.Application.Features.Listings.Queries;
            using EcommercePlatform.Application.Interfaces.Repositories;
            using EcommercePlatform.Application.Interfaces.Services;
            using EcommercePlatform.Domain.Entities; // For GeoLocation
            using Microsoft.Extensions.Logging;
            using System;
            using System.Collections.Generic; // For Dictionary
            using System.Threading;
            using System.Threading.Tasks;

            namespace EcommercePlatform.Application.Features.Listings.Handlers;
            public class GetListingByIdQueryHandler
            {
                private readonly IListingRepository _listingRepository;
                private readonly IUserRepository _userRepository;
                private readonly ICacheService _cacheService;
                private readonly ILogger<GetListingByIdQueryHandler> _logger;

                public GetListingByIdQueryHandler(IListingRepository listingRepository, IUserRepository userRepository, ICacheService cacheService, ILogger<GetListingByIdQueryHandler> logger)
                {
                    _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
                    _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
                    _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
                    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                }

                public async Task<ListingItemViewModel?> HandleAsync(GetListingByIdQuery request, CancellationToken cancellationToken)
                {
                    if (request == null || string.IsNullOrWhiteSpace(request.Id)) throw new ArgumentException("Listing ID must be provided.", nameof(request));
                    string cacheKey = $"listing:{request.Id}";
                    var cachedListing = await _cacheService.GetAsync<ListingItemViewModel>(cacheKey);
                    if (cachedListing != null) {
                        _logger.LogInformation("Cache hit for ListingId: {ListingId}", request.Id);
                        return cachedListing;
                    }
                    _logger.LogInformation("Cache miss for ListingId: {ListingId}", request.Id);
                    var listingItem = await _listingRepository.GetByIdAsync(request.Id);
                    if (listingItem == null) return null;
                    var seller = await _userRepository.GetByIdAsync(listingItem.SellerId);

                    var viewModel = new ListingItemViewModel {
                        Id = listingItem.Id, SellerId = listingItem.SellerId, SellerUsername = seller?.Username ?? "N/A",
                        Title = listingItem.Title, Description = listingItem.Description, Category = listingItem.Category,
                        Price = listingItem.Price, Currency = listingItem.Currency, Condition = listingItem.Condition,
                        ItemSpecifics = listingItem.ItemSpecifics, ImageUrls = listingItem.ImageUrls, Status = listingItem.Status,
                        Tags = listingItem.Tags, Location = listingItem.Location != null ? new GeoLocationViewModel { Longitude = listingItem.Location.Longitude, Latitude = listingItem.Location.Latitude } : null,
                        CreatedAt = listingItem.CreatedAt, UpdatedAt = listingItem.UpdatedAt
                    };
                    await _cacheService.SetAsync(cacheKey, viewModel, TimeSpan.FromMinutes(10));
                    return viewModel;
                }
            }