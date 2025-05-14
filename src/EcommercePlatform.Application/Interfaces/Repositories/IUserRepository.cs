using EcommercePlatform.Domain.Entities;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Repositories;

public interface IUserRepository // Consider inheriting from IGenericRepository<User>
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
}