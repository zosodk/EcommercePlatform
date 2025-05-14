using EcommercePlatform.Application.Interfaces.Repositories;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EcommercePlatform.Infrastructure.Persistence.Repositories;

public class EfCoreUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public EfCoreUserRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        await _context.Users.AddAsync(user);
        return user;
    }

    public Task<bool> UpdateAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        _context.Entry(user).State = EntityState.Modified;
        return Task.FromResult(true);
    }
}