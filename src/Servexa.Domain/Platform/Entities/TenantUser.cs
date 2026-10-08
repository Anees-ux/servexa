using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// TenantUser represents a person's tenant-scoped membership and identity link.
/// Distinct from Resource (scheduling capacity) and Contact (customer relationship).
/// </summary>
public class TenantUser
{
    // Parameterless constructor for EF Core instantiation
    private TenantUser()
    {
    }

    public TenantUser(
        Guid tenantId,
        string externalIssuer,
        string externalSubject,
        string displayName,
        string? normalizedEmail = null,
        Guid? defaultBranchId = null,
        TenantUserStatus status = TenantUserStatus.Active,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(externalIssuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ExternalIssuer = externalIssuer.Trim();
        ExternalSubject = externalSubject.Trim();
        DisplayName = displayName.Trim();
        NormalizedEmail = string.IsNullOrWhiteSpace(normalizedEmail) ? null : normalizedEmail.Trim().ToUpperInvariant();
        DefaultBranchId = defaultBranchId;
        Status = status;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ExternalIssuer { get; private set; } = null!;
    public string ExternalSubject { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string? NormalizedEmail { get; private set; }
    public Guid? DefaultBranchId { get; private set; }
    public TenantUserStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateProfile(string displayName, string? normalizedEmail, Guid? defaultBranchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName.Trim();
        NormalizedEmail = string.IsNullOrWhiteSpace(normalizedEmail) ? null : normalizedEmail.Trim().ToUpperInvariant();
        DefaultBranchId = defaultBranchId;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(TenantUserStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void SetDefaultBranch(Guid? defaultBranchId)
    {
        DefaultBranchId = defaultBranchId;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
