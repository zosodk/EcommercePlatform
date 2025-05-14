using EcommercePlatform.Application.Features.Listings.Commands;
            using EcommercePlatform.Application.Interfaces.Repositories;
            using EcommercePlatform.Application.Interfaces.Common; // For IUnitOfWork
            using EcommercePlatform.Domain.Entities;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.Logging;
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            namespace EcommercePlatform.Application.Features.Listings.Handlers;
            public class CreateListingCommandHandler
            {
                private readonly IListingRepository _listingRepository;
                private readonly IUserRepository _userRepository;
                private readonly IConfiguration _configuration;
                private readonly IUnitOfWork _unitOfWork; // Key change: Use IUnitOfWork
                private readonly ILogger<CreateListingCommandHandler> _logger;

                public CreateListingCommandHandler(
                    IListingRepository listingRepository, IUserRepository userRepository,
                    IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<CreateListingCommandHandler> logger)
                {
                    _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
                    _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
                    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
                    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                }

                public async Task<string> HandleAsync(CreateListingCommand request, CancellationToken cancellationToken)
                {
                    if (request == null) throw new ArgumentNullException(nameof(request));
                    var seller = await _userRepository.GetByIdAsync(request.SellerId);
                    if (seller == null) throw new ApplicationException($"Seller with ID {request.SellerId} not found.");

                    var s3BucketName = _configuration["AWS:S3BucketName"];
                    var awsRegion = _configuration["AWS:Region"];
                    var serviceUrl = _configuration["AWS:ServiceURL"];
                    string s3BucketBaseUrl;
                    if (!string.IsNullOrEmpty(serviceUrl) && serviceUrl.Contains("minio", StringComparison.OrdinalIgnoreCase))
                        s3BucketBaseUrl = $"{serviceUrl.TrimEnd('/')}/{s3BucketName}";
                    else if(!string.IsNullOrEmpty(s3BucketName) && !string.IsNullOrEmpty(awsRegion))
                        s3BucketBaseUrl = $"https://{s3BucketName}.s3.{awsRegion}.amazonaws.com";
                    else throw new InvalidOperationException("Image storage service URL is not configured correctly.");

                    var imageUrls = request.ImageObjectKeys?.Where(k => !string.IsNullOrWhiteSpace(k)).Select(key => $"{s3BucketBaseUrl.TrimEnd('/')}/{key.TrimStart('/')}").ToList() ?? new List<string>();
                    GeoLocation? location = (request.LocationLongitude.HasValue && request.LocationLatitude.HasValue)
                        ? GeoLocation.Create(request.LocationLongitude.Value, request.LocationLatitude.Value) : null;

                    var listingItem = new ListingItem {
                        SellerId = request.SellerId, Title = request.Title, Description = request.Description,
                        Category = request.Category, Price = request.Price, Currency = request.Currency,
                        Condition = request.Condition, ItemSpecifics = request.ItemSpecifics ?? new Dictionary<string, object>(),
                        ImageUrls = imageUrls, Status = ListingStatus.Available, Tags = request.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
                        Location = location, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Version = 1
                    };

                    await _listingRepository.CreateAsync(listingItem); // Adds to DbContext's change tracker
                    await _unitOfWork.SaveChangesAsync(cancellationToken); // Persists changes via Unit of Work

                    _logger.LogInformation("Listing created: {ListingId} by Seller: {SellerId}", listingItem.Id, listingItem.SellerId);
                    return listingItem.Id;
                }
            }