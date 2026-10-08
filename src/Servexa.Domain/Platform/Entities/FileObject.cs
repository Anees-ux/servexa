using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// FileObject represents SQL-side metadata for binary objects stored in object storage (ADR-007).
/// No binary payload bytes are stored in SQL Server.
/// </summary>
public class FileObject
{
    // Parameterless constructor for EF Core instantiation
    private FileObject()
    {
    }

    public FileObject(
        Guid tenantId,
        string ownerType,
        Guid ownerId,
        string originalName,
        string contentType,
        string storageKey,
        string classification,
        long sizeBytes = 0,
        FileVisibility visibility = FileVisibility.Internal,
        Guid? uploadedByUserId = null,
        byte[]? contentHash = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(classification);

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "SizeBytes cannot be negative.");
        }

        if (contentHash is not null && contentHash.Length != 32)
        {
            throw new ArgumentException("ContentHash must be exactly 32 bytes (SHA-256).", nameof(contentHash));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        OwnerType = ownerType.Trim();
        OwnerId = ownerId;
        OriginalName = originalName.Trim();
        ContentType = contentType.Trim().ToLowerInvariant();
        StorageKey = storageKey.Trim();
        Classification = classification.Trim();
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        Visibility = visibility;
        UploadStatus = FileUploadStatus.Initiated;
        ScanStatus = FileScanStatus.PendingScan;
        RetentionState = FileRetentionState.Active;
        UploadedByUserId = uploadedByUserId;
        UploadedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = UploadedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string OwnerType { get; private set; } = null!;
    public Guid OwnerId { get; private set; }
    public string OriginalName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public byte[]? ContentHash { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string Classification { get; private set; } = null!;
    public FileVisibility Visibility { get; private set; }
    public FileUploadStatus UploadStatus { get; private set; }
    public FileScanStatus ScanStatus { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public FileRetentionState RetentionState { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void MarkUploaded(long sizeBytes, byte[]? contentHash = null)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Uploaded SizeBytes must be positive.");
        }

        if (contentHash is not null && contentHash.Length != 32)
        {
            throw new ArgumentException("ContentHash must be exactly 32 bytes (SHA-256).", nameof(contentHash));
        }

        SizeBytes = sizeBytes;
        if (contentHash is not null)
        {
            ContentHash = contentHash;
        }

        UploadStatus = FileUploadStatus.Uploaded;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkUploadFailed()
    {
        UploadStatus = FileUploadStatus.Failed;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateScanStatus(FileScanStatus newScanStatus)
    {
        ScanStatus = newScanStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void SetVisibility(FileVisibility visibility)
    {
        Visibility = visibility;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void SetRetentionState(FileRetentionState newState)
    {
        RetentionState = newState;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
