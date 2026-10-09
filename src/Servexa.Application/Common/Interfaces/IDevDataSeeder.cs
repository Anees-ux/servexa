namespace Servexa.Application.Common.Interfaces;

public interface IDevDataSeeder
{
    Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default);
}
