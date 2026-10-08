using System.Text.Json;
using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// PolicySetting represents tenant-configurable policy (PLT-004, PLT-008).
/// Policy history is preserved; updates are effective-dated version rows.
/// </summary>
public class PolicySetting
{
    // Parameterless constructor for EF Core instantiation
    private PolicySetting()
    {
    }

    public PolicySetting(
        Guid tenantId,
        string policyKey,
        ScopeType scopeType,
        string valueJson,
        DateTime effectiveFromUtc,
        int versionNumber,
        Guid? scopeId = null,
        DateTime? effectiveToUtc = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(policyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueJson);

        if (versionNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "VersionNumber must be at least 1.");
        }

        if (effectiveToUtc.HasValue && effectiveToUtc.Value < effectiveFromUtc)
        {
            throw new ArgumentException("EffectiveToUtc cannot be earlier than EffectiveFromUtc.", nameof(effectiveToUtc));
        }

        // Validate that valueJson is well-formed JSON
        try
        {
            using var _ = JsonDocument.Parse(valueJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("ValueJson must be valid JSON.", nameof(valueJson), ex);
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        PolicyKey = policyKey.Trim();
        ScopeType = scopeType;
        ScopeId = scopeId;
        ValueJson = valueJson.Trim();
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        VersionNumber = versionNumber;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string PolicyKey { get; private set; } = null!;
    public ScopeType ScopeType { get; private set; }
    public Guid? ScopeId { get; private set; }
    public string ValueJson { get; private set; } = null!;
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public int VersionNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void Retire(DateTime effectiveToUtc)
    {
        if (effectiveToUtc < EffectiveFromUtc)
        {
            throw new ArgumentException("EffectiveToUtc cannot be earlier than EffectiveFromUtc.", nameof(effectiveToUtc));
        }

        EffectiveToUtc = effectiveToUtc;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public bool IsActive(DateTime? asOfUtc = null)
    {
        var instant = asOfUtc ?? DateTime.UtcNow;
        return EffectiveFromUtc <= instant && (!EffectiveToUtc.HasValue || EffectiveToUtc.Value >= instant);
    }
}
