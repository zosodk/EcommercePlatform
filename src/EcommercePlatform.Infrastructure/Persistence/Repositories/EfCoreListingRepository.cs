using EcommercePlatform.Application.Interfaces.Repositories;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

    namespace EcommercePlatform.Infrastructure.Persistence.Repositories;

    public class EfCoreListingRepository : IListingRepository
    {
        private readonly AppDbContext _context;

        public EfCoreListingRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ListingItem?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _context.Listings.FindAsync(id);
        }

        public async Task<ListingItem> CreateAsync(ListingItem listing)
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));
            await _context.Listings.AddAsync(listing);
            // SaveChangesAsync is called by UnitOfWork
            return listing;
        }

        public Task<bool> UpdateAsync(ListingItem listing) // EF Core tracks changes, UoW saves.
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));
            _context.Entry(listing).State = EntityState.Modified;
            return Task.FromResult(true);
        }

        public async Task<List<ListingItem>> FindAsync(Expression<Func<ListingItem, bool>> predicate, int skip, int limit)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
            return await _context.Listings.Where(predicate).Skip(skip).Take(limit).ToListAsync();
        }

        public async Task<ListingItem?> GetByIdAndStatusAsync(string id, string status)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(status)) return null;
            return await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.Status == status);
        }
    }