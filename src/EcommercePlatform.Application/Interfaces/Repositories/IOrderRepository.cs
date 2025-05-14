using EcommercePlatform.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Repositories;

public interface IOrderRepository // Consider inheriting from IGenericRepository<Order>
{
    Task<Order?> GetByIdAsync(string id);
    Task<Order> CreateAsync(Order order);
    Task<bool> UpdateAsync(Order order);
    Task<List<Order>> GetOrdersByBuyerIdAsync(string buyerId, int page, int pageSize);
    Task<List<Order>> GetOrdersBySellerIdAsync(string sellerId, int page, int pageSize);
}