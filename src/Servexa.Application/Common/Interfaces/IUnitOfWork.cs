namespace Servexa.Application.Common.Interfaces;

/// <summary>
/// Unit of Work contract coordinating transaction commits.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
