using EcommercePlatform.Application.Interfaces.Repositories;
    using EcommercePlatform.Domain.Entities;
    using EcommercePlatform.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    namespace EcommercePlatform.Infrastructure.Persistence.Repositories;

    public class EfCoreReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public EfCoreReviewRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Review?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _context.Reviews.FindAsync(id);
        }

        public async Task<Review> CreateAsync(Review review)
        {
            if (review == null) throw new ArgumentNullException(nameof(review));
            await _context.Reviews.AddAsync(review);
            return review;
        }

        public async Task<List<Review>> GetReviewsBySellerIdAsync(string sellerId, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return new List<Review>();
            return await _context.Reviews
                                 .Where(r => r.SellerId == sellerId)
                                 .OrderByDescending(r => r.CreatedAt)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }

        public async Task<List<Review>> GetReviewsByListingIdAsync(string listingId, int page, int pageSize)
        {
             if (string.IsNullOrWhiteSpace(listingId)) return new List<Review>();
             return await _context.Reviews
                                 .Where(r => r.ListingId == listingId)
                                 .OrderByDescending(r => r.CreatedAt)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }
    }