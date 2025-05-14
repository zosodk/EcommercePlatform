using EcommercePlatform.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Repositories;

public interface IReviewRepository // Consider inheriting from IGenericRepository<Review>
{
    Task<Review?> GetByIdAsync(string id);
    Task<Review> CreateAsync(Review review);
    Task<List<Review>> GetReviewsBySellerIdAsync(string sellerId, int page, int pageSize);
    Task<List<Review>> GetReviewsByListingIdAsync(string listingId, int page, int pageSize);
}