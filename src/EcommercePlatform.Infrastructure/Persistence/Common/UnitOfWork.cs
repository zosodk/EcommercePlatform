// Purpose: Implements the IUnitOfWork interface using the AppDbContext.
// This class resides in the Infrastructure layer and handles the actual database operations.

using EcommercePlatform.Application.Interfaces.Common;
using EcommercePlatform.Infrastructure.Persistence; // For AppDbContext
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcommercePlatform.Infrastructure.Persistence.Common;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    // private IListingRepository _listingRepository;
    // private IOrderRepository _orderRepository;


    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // lazy-loaded repositories if exposed via IUnitOfWork:
    // public IListingRepository Listings => _listingRepository ??= new EfCoreListingRepository(_dbContext);
    // public IOrderRepository Orders => _orderRepository ??= new EfCoreOrderRepository(_dbContext);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null for commit.");
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null for rollback.");
        }
        await transaction.RollbackAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // This will dispose the DbContext when the UnitOfWork is disposed.
        // This is important if the UnitOfWork is scoped (e.g., per HTTP request).
        await _dbContext.DisposeAsync();
        GC.SuppressFinalize(this); // Suppress finalization if DisposeAsync is called
    }
}