using EcommercePlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Repositories;

public interface IListingRepository // Consider inheriting from IGenericRepository<ListingItem>
{
    Task<ListingItem?> GetByIdAsync(string id);
    Task<ListingItem> CreateAsync(ListingItem listing);
    Task<bool> UpdateAsync(ListingItem listing); // Returns bool indicating if update was prepared/successful at repo level
    Task<List<ListingItem>> FindAsync(Expression<Func<ListingItem, bool>> predicate, int skip, int limit);
    Task<ListingItem?> GetByIdAndStatusAsync(string id, string status);
    // Add other listing-specific methods, e.g., GetBySellerIdAsync
}