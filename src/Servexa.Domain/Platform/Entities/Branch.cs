using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// Branch represents an organizational / operating unit within a Tenant.
/// </summary>
public class Branch
{
    // Parameterless constructor for EF Core instantiation
    private Branch()
    {
    }

    public Branch(
        Guid tenantId,
        string code,
        string name,
        string timeZoneId,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        Code = code.Trim();
        Name = name.Trim();
        TimeZoneId = timeZoneId.Trim();
        Status = BranchStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public BranchStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateDetails(string name, string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        Name = name.Trim();
        TimeZoneId = timeZoneId.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(BranchStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
