using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// NumberSeries provides tenant-scoped issuance of human-facing business numbers
/// (e.g. Account, WO, Invoice, Booking).
/// Number issuance is protected by optimistic concurrency and short transaction locking.
/// </summary>
public class NumberSeries
{
    // Parameterless constructor for EF Core instantiation
    private NumberSeries()
    {
    }

    public NumberSeries(
        Guid tenantId,
        string seriesKey,
        string scopeKey = "DEFAULT",
        string? prefixPattern = null,
        long startValue = 1,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(seriesKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeKey);

        if (startValue < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startValue), "StartValue must be greater than or equal to 1.");
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        SeriesKey = seriesKey.Trim().ToUpperInvariant();
        ScopeKey = scopeKey.Trim().ToUpperInvariant();
        PrefixPattern = string.IsNullOrWhiteSpace(prefixPattern) ? null : prefixPattern.Trim();
        NextValue = startValue;
        Status = NumberSeriesStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string SeriesKey { get; private set; } = null!;
    public string ScopeKey { get; private set; } = null!;
    public string? PrefixPattern { get; private set; }
    public long NextValue { get; private set; }
    public NumberSeriesStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public long AllocateNextValue()
    {
        if (Status != NumberSeriesStatus.Active)
        {
            throw new InvalidOperationException($"Cannot allocate number from series '{SeriesKey}' because it is not active.");
        }

        var allocated = NextValue;
        NextValue++;
        ModifiedAtUtc = DateTime.UtcNow;
        return allocated;
    }

    public void UpdateDetails(string? prefixPattern)
    {
        PrefixPattern = string.IsNullOrWhiteSpace(prefixPattern) ? null : prefixPattern.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(NumberSeriesStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
