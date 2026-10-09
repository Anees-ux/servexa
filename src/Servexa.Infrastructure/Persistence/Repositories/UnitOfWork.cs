using Servexa.Application.Common.Interfaces;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(ServexaDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
