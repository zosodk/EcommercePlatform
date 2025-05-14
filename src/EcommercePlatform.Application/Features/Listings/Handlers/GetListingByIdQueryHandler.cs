    using EcommercePlatform.Application.Interfaces.Repositories;
    using EcommercePlatform.Application.Interfaces.Services; // For ICacheService
    using EcommercePlatform.Domain.Entities;
    using System.Threading.Tasks; // For Task
    using System.Threading; // For CancellationToken
    using System; // For TimeSpan, ArgumentNullException
    using System.Collections.Generic;
    using EcommercePlatform.Application.Features.Listings.Queries; // For Dictionary, List
    // using MediatR; // If using MediatR: public class GetListingByIdQueryHandler : IRequestHandler<GetListingByIdQuery, ListingItemViewModel>

    namespace EcommercePlatform.Application.Features.Listings.Handlers;

    public class GetListingByIdQueryHandler // : IRequestHandler<GetListingByIdQuery, ListingItemViewModel>
    {
        private readonly IListingRepository _listingRepository;
        private readonly IUserRepository _userRepository; // To get seller username
        private readonly ICacheService _cacheService;

        public GetListingByIdQueryHandler(
            IListingRepository listingRepository,
            IUserRepository userRepository,
            ICacheService cacheService)
        {
            _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        // public async Task<ListingItemViewModel?> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
        public async Task<ListingItemViewModel?> HandleAsync(GetListingByIdQuery request) // Simplified
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Id)) throw new ArgumentException("Listing ID cannot be empty.", nameof(request.Id));

            string cacheKey = $"listing:{request.Id}";

            // 1. Try to get from cache
            var cachedListingViewModel = await _cacheService.GetAsync<ListingItemViewModel>(cacheKey);
            if (cachedListingViewModel != null)
            {
                return cachedListingViewModel;
            }

            // 2. Get from repository if not in cache
            var listingItem = await _listingRepository.GetByIdAsync(request.Id);
            if (listingItem == null)
            {
                return null; // Or throw NotFoundException
            }

            // 3. Get additional data if needed (e.g., seller username)
            var seller = await _userRepository.GetByIdAsync(listingItem.SellerId);
            var sellerUsername = seller?.Username ?? "Unknown Seller";

            // 4. Map to ViewModel
            var viewModel = new ListingItemViewModel
            {
                Id = listingItem.Id,
                SellerId = listingItem.SellerId,
                SellerUsername = sellerUsername,
                Title = listingItem.Title,
                Description = listingItem.Description,
                Category = listingItem.Category,
                Price = listingItem.Price,
                Currency = listingItem.Currency,
                Condition = listingItem.Condition,
                ItemSpecifics = listingItem.ItemSpecifics ?? new Dictionary<string, object>(),
                ImageUrls = listingItem.ImageUrls ?? new List<string>(),
                Status = listingItem.Status,
                Tags = listingItem.Tags,
                Location = listingItem.Location != null
                    ? new GeoLocationViewModel { Longitude = listingItem.Location.Longitude, Latitude = listingItem.Location.Latitude }
                    : null,
                CreatedAt = listingItem.CreatedAt,
                UpdatedAt = listingItem.UpdatedAt
            };

            // 5. Store in cache Redus
            await _cacheService.SetAsync(cacheKey, viewModel, TimeSpan.FromMinutes(10)); // Cache for 10 minutes

            return viewModel;
        }
    }