using Microsoft.EntityFrameworkCore.Storage; // For IDbContextTransaction
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Common;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}