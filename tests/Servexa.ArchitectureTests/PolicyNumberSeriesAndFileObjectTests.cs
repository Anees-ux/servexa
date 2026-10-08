using Microsoft.EntityFrameworkCore;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Platform.Enums;
using Servexa.Infrastructure.Persistence;

namespace Servexa.ArchitectureTests;

public class PolicyNumberSeriesAndFileObjectTests
{
    #region PolicySetting Tests

    [Fact]
    public void PolicySetting_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var key = " scheduling.overtime_threshold ";
        var json = """{"maxHours": 10, "requireApproval": true}""";
        var fromUtc = DateTime.UtcNow.AddDays(-1);

        // Act
        var policy = new PolicySetting(tenantId, key, ScopeType.Branch, json, fromUtc, 1);

        // Assert
        Assert.NotEqual(Guid.Empty, policy.Id);
        Assert.Equal(tenantId, policy.TenantId);
        Assert.Equal("scheduling.overtime_threshold", policy.PolicyKey);
        Assert.Equal(ScopeType.Branch, policy.ScopeType);
        Assert.Null(policy.ScopeId);
        Assert.Equal(json, policy.ValueJson);
        Assert.Equal(fromUtc, policy.EffectiveFromUtc);
        Assert.Null(policy.EffectiveToUtc);
        Assert.Equal(1, policy.VersionNumber);
        Assert.True(policy.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Equal(policy.CreatedAtUtc, policy.ModifiedAtUtc);
        Assert.True(policy.IsActive());
    }

    [Fact]
    public void PolicySetting_Creation_WithInvalidJson_ShouldThrowArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new PolicySetting(Guid.NewGuid(), "key", ScopeType.Tenant, "{invalid-json}", DateTime.UtcNow, 1));
    }

    [Fact]
    public void PolicySetting_Creation_WithInvalidVersion_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PolicySetting(Guid.NewGuid(), "key", ScopeType.Tenant, "{}", DateTime.UtcNow, 0));
    }

    [Fact]
    public void PolicySetting_Creation_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PolicySetting(Guid.Empty, "key", ScopeType.Tenant, "{}", DateTime.UtcNow, 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PolicySetting_Creation_WithInvalidKey_ShouldThrowArgumentException(string? invalidKey)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new PolicySetting(Guid.NewGuid(), invalidKey!, ScopeType.Tenant, "{}", DateTime.UtcNow, 1));
    }

    [Fact]
    public void PolicySetting_Retire_ShouldSetEffectiveToUtc()
    {
        // Arrange
        var fromUtc = DateTime.UtcNow.AddDays(-10);
        var toUtc = DateTime.UtcNow;
        var policy = new PolicySetting(Guid.NewGuid(), "key", ScopeType.Tenant, "{}", fromUtc, 1);

        // Act
        policy.Retire(toUtc);

        // Assert
        Assert.Equal(toUtc, policy.EffectiveToUtc);
        Assert.False(policy.IsActive(DateTime.UtcNow.AddDays(1)));
    }

    #endregion

    #region NumberSeries Tests

    [Fact]
    public void NumberSeries_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var series = new NumberSeries(tenantId, " work_order ", " branch_1 ", "WO-{YYYY}-", 100);

        // Assert
        Assert.NotEqual(Guid.Empty, series.Id);
        Assert.Equal(tenantId, series.TenantId);
        Assert.Equal("WORK_ORDER", series.SeriesKey);
        Assert.Equal("BRANCH_1", series.ScopeKey);
        Assert.Equal("WO-{YYYY}-", series.PrefixPattern);
        Assert.Equal(100, series.NextValue);
        Assert.Equal(NumberSeriesStatus.Active, series.Status);
        Assert.True(series.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Equal(series.CreatedAtUtc, series.ModifiedAtUtc);
    }

    [Fact]
    public void NumberSeries_AllocateNextValue_ShouldIncrementCounter()
    {
        // Arrange
        var series = new NumberSeries(Guid.NewGuid(), "INV", startValue: 1000);

        // Act
        var val1 = series.AllocateNextValue();
        var val2 = series.AllocateNextValue();

        // Assert
        Assert.Equal(1000, val1);
        Assert.Equal(1001, val2);
        Assert.Equal(1002, series.NextValue);
    }

    [Fact]
    public void NumberSeries_AllocateNextValue_InactiveSeries_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var series = new NumberSeries(Guid.NewGuid(), "INV");
        series.UpdateStatus(NumberSeriesStatus.Inactive);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => series.AllocateNextValue());
    }

    [Fact]
    public void NumberSeries_Creation_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new NumberSeries(Guid.Empty, "KEY"));
    }

    [Fact]
    public void NumberSeries_Creation_WithInvalidStartValue_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumberSeries(Guid.NewGuid(), "KEY", startValue: 0));
    }

    #endregion

    #region FileObject Tests

    [Fact]
    public void FileObject_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var file = new FileObject(
            tenantId,
            " WorkOrder ",
            ownerId,
            " photo.jpg ",
            " IMAGE/JPEG ",
            " tenants/1/wo/1.jpg ",
            " Evidence ",
            sizeBytes: 1024,
            visibility: FileVisibility.CustomerVisible,
            uploadedByUserId: userId);

        // Assert
        Assert.NotEqual(Guid.Empty, file.Id);
        Assert.Equal(tenantId, file.TenantId);
        Assert.Equal("WorkOrder", file.OwnerType);
        Assert.Equal(ownerId, file.OwnerId);
        Assert.Equal("photo.jpg", file.OriginalName);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal("tenants/1/wo/1.jpg", file.StorageKey);
        Assert.Equal("Evidence", file.Classification);
        Assert.Equal(1024, file.SizeBytes);
        Assert.Equal(FileVisibility.CustomerVisible, file.Visibility);
        Assert.Equal(FileUploadStatus.Initiated, file.UploadStatus);
        Assert.Equal(FileScanStatus.PendingScan, file.ScanStatus);
        Assert.Equal(FileRetentionState.Active, file.RetentionState);
        Assert.Equal(userId, file.UploadedByUserId);
        Assert.Null(file.ContentHash);
    }

    [Fact]
    public void FileObject_MarkUploaded_ShouldUpdateStatusAndHash()
    {
        // Arrange
        var file = new FileObject(
            Guid.NewGuid(), "Evidence", Guid.NewGuid(), "test.pdf", "application/pdf", "key", "Attachment");
        var hash = new byte[32];
        Array.Fill<byte>(hash, 0xAB);

        // Act
        file.MarkUploaded(2048, hash);

        // Assert
        Assert.Equal(FileUploadStatus.Uploaded, file.UploadStatus);
        Assert.Equal(2048, file.SizeBytes);
        Assert.NotNull(file.ContentHash);
        Assert.Equal(32, file.ContentHash.Length);
        Assert.Equal(hash, file.ContentHash);
    }

    [Fact]
    public void FileObject_Creation_WithInvalidHashLength_ShouldThrowArgumentException()
    {
        var invalidHash = new byte[16]; // Not 32 bytes

        Assert.Throws<ArgumentException>(() =>
            new FileObject(
                Guid.NewGuid(), "Evidence", Guid.NewGuid(), "test.pdf", "application/pdf", "key", "Attachment",
                contentHash: invalidHash));
    }

    [Fact]
    public void FileObject_Creation_WithEmptyOwnerId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new FileObject(Guid.NewGuid(), "Evidence", Guid.Empty, "test.pdf", "application/pdf", "key", "Attachment"));
    }

    [Fact]
    public void FileObject_UpdateLifecycleStates_ShouldUpdateFields()
    {
        // Arrange
        var file = new FileObject(
            Guid.NewGuid(), "Evidence", Guid.NewGuid(), "test.pdf", "application/pdf", "key", "Attachment");

        // Act
        file.MarkUploadFailed();
        Assert.Equal(FileUploadStatus.Failed, file.UploadStatus);

        file.UpdateScanStatus(FileScanStatus.Available);
        Assert.Equal(FileScanStatus.Available, file.ScanStatus);

        file.SetRetentionState(FileRetentionState.LegalHold);
        Assert.Equal(FileRetentionState.LegalHold, file.RetentionState);

        file.SetVisibility(FileVisibility.CustomerVisible);
        Assert.Equal(FileVisibility.CustomerVisible, file.Visibility);
    }

    #endregion

    #region EF Core Persistence Model Metadata Tests

    [Fact]
    public void DbContext_Model_ShouldContainCorrectPolicyNumberSeriesAndFileObjectsMappings()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseSqlServer("Server=tcp:localhost,1433;Database=Servexa_Test;User Id=sa;Password=Test!;TrustServerCertificate=True")
            .Options;

        using var context = new ServexaDbContext(options);
        var model = context.Model;

        // 1. PolicySetting
        var psType = model.FindEntityType(typeof(PolicySetting));
        Assert.NotNull(psType);
        Assert.Equal("PolicySettings", psType.GetTableName());
        Assert.Equal("platform", psType.GetSchema());
        var psAk = psType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(PolicySetting.TenantId), nameof(PolicySetting.Id) }));
        Assert.NotNull(psAk);
        var psUniqueIndex = psType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).Contains(nameof(PolicySetting.PolicyKey)));
        Assert.NotNull(psUniqueIndex);

        // 2. NumberSeries
        var nsType = model.FindEntityType(typeof(NumberSeries));
        Assert.NotNull(nsType);
        Assert.Equal("NumberSeries", nsType.GetTableName());
        Assert.Equal("platform", nsType.GetSchema());
        var nsAk = nsType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(NumberSeries.TenantId), nameof(NumberSeries.Id) }));
        Assert.NotNull(nsAk);
        var nsUniqueIndex = nsType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(NumberSeries.TenantId), nameof(NumberSeries.SeriesKey), nameof(NumberSeries.ScopeKey) }));
        Assert.NotNull(nsUniqueIndex);

        // 3. FileObject
        var foType = model.FindEntityType(typeof(FileObject));
        Assert.NotNull(foType);
        Assert.Equal("FileObjects", foType.GetTableName());
        Assert.Equal("platform", foType.GetSchema());
        var foAk = foType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FileObject.TenantId), nameof(FileObject.Id) }));
        Assert.NotNull(foAk);
        var foUniqueStorageKey = foType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FileObject.TenantId), nameof(FileObject.StorageKey) }));
        Assert.NotNull(foUniqueStorageKey);

        var foOwnerIndex = foType.GetIndexes()
            .FirstOrDefault(i => !i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FileObject.TenantId), nameof(FileObject.OwnerType), nameof(FileObject.OwnerId) }));
        Assert.NotNull(foOwnerIndex);

        var userFk = foType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TenantUser));
        Assert.NotNull(userFk);
        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);
    }

    #endregion
}
