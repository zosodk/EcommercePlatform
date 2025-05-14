using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity); // For EF Core, this might just mark state as modified
    Task DeleteAsync(T entity); // For EF Core, this might just mark state as deleted
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    // Add other common methods if needed, e.g., CountAsync
}