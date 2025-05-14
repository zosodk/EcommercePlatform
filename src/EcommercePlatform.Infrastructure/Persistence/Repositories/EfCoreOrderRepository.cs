using EcommercePlatform.Application.Interfaces.Repositories;
    using EcommercePlatform.Domain.Entities;
    using EcommercePlatform.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    namespace EcommercePlatform.Infrastructure.Persistence.Repositories;

    public class EfCoreOrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public EfCoreOrderRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Order?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _context.Orders
                                 // .Include(o => o.Items) // If Items were separate and needed include (not for owned)
                                 .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> CreateAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            await _context.Orders.AddAsync(order);
            return order;
        }

        public Task<bool> UpdateAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            _context.Entry(order).State = EntityState.Modified;
            return Task.FromResult(true);
        }

        public async Task<List<Order>> GetOrdersByBuyerIdAsync(string buyerId, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(buyerId)) return new List<Order>();
            return await _context.Orders
                                 .Where(o => o.BuyerId == buyerId)
                                 .OrderByDescending(o => o.CreatedAt)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersBySellerIdAsync(string sellerId, int page, int pageSize)
        {
             if (string.IsNullOrWhiteSpace(sellerId)) return new List<Order>();
             return await _context.Orders
                                 .Where(o => o.SellerId == sellerId) // Assuming SellerId is directly on Order
                                 // Or query through Items if SellerId is only on Listing
                                 // .Where(o => o.Items.Any(i => /* logic to link item to seller */))
                                 .OrderByDescending(o => o.CreatedAt)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }
    }