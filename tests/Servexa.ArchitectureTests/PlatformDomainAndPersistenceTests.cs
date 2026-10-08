using Microsoft.EntityFrameworkCore;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Platform.Enums;
using Servexa.Infrastructure.Persistence;

namespace Servexa.ArchitectureTests;

public class PlatformDomainAndPersistenceTests
{
    [Fact]
    public void Territory_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var code = " US-EAST ";
        var name = " US Eastern Region ";

        // Act
        var territory = new Territory(tenantId, code, name);

        // Assert
        Assert.NotEqual(Guid.Empty, territory.Id);
        Assert.Equal(tenantId, territory.TenantId);
        Assert.Equal("US-EAST", territory.Code);
        Assert.Equal("US Eastern Region", territory.Name);
        Assert.Null(territory.ParentTerritoryId);
        Assert.Equal(TerritoryStatus.Active, territory.Status);
        Assert.True(territory.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Equal(territory.CreatedAtUtc, territory.ModifiedAtUtc);
    }

    [Fact]
    public void Territory_Creation_WithParent_ShouldSetParentTerritoryId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        // Act
        var territory = new Territory(tenantId, "US-NE", "Northeast", parentId);

        // Assert
        Assert.Equal(parentId, territory.ParentTerritoryId);
    }

    [Fact]
    public void Territory_Creation_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Territory(Guid.Empty, "CODE", "Name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Territory_Creation_WithInvalidCode_ShouldThrowArgumentException(string? invalidCode)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Territory(Guid.NewGuid(), invalidCode!, "Name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Territory_Creation_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Territory(Guid.NewGuid(), "CODE", invalidName!));
    }

    [Fact]
    public void Territory_Creation_SelfParenting_ShouldThrowArgumentException()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Territory(Guid.NewGuid(), "CODE", "Name", parentTerritoryId: id, id: id));
    }

    [Fact]
    public void Territory_UpdateDetails_ShouldUpdatePropertiesAndTimestamps()
    {
        // Arrange
        var territory = new Territory(Guid.NewGuid(), "CODE", "Old Name");
        var parentId = Guid.NewGuid();

        // Act
        territory.UpdateDetails(" New Name ", parentId);

        // Assert
        Assert.Equal("New Name", territory.Name);
        Assert.Equal(parentId, territory.ParentTerritoryId);
    }

    [Fact]
    public void Territory_UpdateDetails_SelfParenting_ShouldThrowArgumentException()
    {
        // Arrange
        var territory = new Territory(Guid.NewGuid(), "CODE", "Name");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => territory.UpdateDetails("New Name", territory.Id));
    }

    [Fact]
    public void Territory_UpdateStatus_ShouldUpdateStatus()
    {
        // Arrange
        var territory = new Territory(Guid.NewGuid(), "CODE", "Name");

        // Act
        territory.UpdateStatus(TerritoryStatus.Inactive);

        // Assert
        Assert.Equal(TerritoryStatus.Inactive, territory.Status);
    }

    [Fact]
    public void TenantUser_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var issuer = " https://auth.servexa.com ";
        var subject = " auth0|123456 ";
        var displayName = " Alice Specialist ";
        var email = " Alice@example.com ";
        var branchId = Guid.NewGuid();

        // Act
        var user = new TenantUser(tenantId, issuer, subject, displayName, email, branchId);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal("https://auth.servexa.com", user.ExternalIssuer);
        Assert.Equal("auth0|123456", user.ExternalSubject);
        Assert.Equal("Alice Specialist", user.DisplayName);
        Assert.Equal("ALICE@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal(branchId, user.DefaultBranchId);
        Assert.Equal(TenantUserStatus.Active, user.Status);
        Assert.True(user.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Equal(user.CreatedAtUtc, user.ModifiedAtUtc);
    }

    [Fact]
    public void TenantUser_Creation_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new TenantUser(Guid.Empty, "issuer", "sub", "Alice"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantUser_Creation_WithInvalidIssuer_ShouldThrowArgumentException(string? invalidIssuer)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new TenantUser(Guid.NewGuid(), invalidIssuer!, "sub", "Alice"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantUser_Creation_WithInvalidSubject_ShouldThrowArgumentException(string? invalidSubject)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new TenantUser(Guid.NewGuid(), "issuer", invalidSubject!, "Alice"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantUser_Creation_WithInvalidDisplayName_ShouldThrowArgumentException(string? invalidName)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new TenantUser(Guid.NewGuid(), "issuer", "sub", invalidName!));
    }

    [Fact]
    public void TenantUser_UpdateProfile_ShouldUpdateFieldsAndTimestamps()
    {
        // Arrange
        var user = new TenantUser(Guid.NewGuid(), "issuer", "sub", "Old Name");
        var branchId = Guid.NewGuid();

        // Act
        user.UpdateProfile(" New Name ", " new@example.com ", branchId);

        // Assert
        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal("NEW@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal(branchId, user.DefaultBranchId);
    }

    [Fact]
    public void TenantUser_UpdateStatus_ShouldUpdateStatus()
    {
        // Arrange
        var user = new TenantUser(Guid.NewGuid(), "issuer", "sub", "Alice");

        // Act
        user.UpdateStatus(TenantUserStatus.Disabled);

        // Assert
        Assert.Equal(TenantUserStatus.Disabled, user.Status);
    }

    [Fact]
    public void TenantUser_SetDefaultBranch_ShouldUpdateBranchId()
    {
        // Arrange
        var user = new TenantUser(Guid.NewGuid(), "issuer", "sub", "Alice");
        var branchId = Guid.NewGuid();

        // Act
        user.SetDefaultBranch(branchId);

        // Assert
        Assert.Equal(branchId, user.DefaultBranchId);

        // Act - clear branch
        user.SetDefaultBranch(null);

        // Assert
        Assert.Null(user.DefaultBranchId);
    }

    [Fact]
    public void DbContext_Model_ShouldContainCorrectPlatformEntitiesAndConfigurations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseSqlServer("Server=tcp:localhost,1433;Database=Servexa_Test;User Id=sa;Password=Test!;TrustServerCertificate=True")
            .Options;

        using var context = new ServexaDbContext(options);
        var model = context.Model;

        // Territory entity verification
        var territoryType = model.FindEntityType(typeof(Territory));
        Assert.NotNull(territoryType);
        Assert.Equal("Territories", territoryType.GetTableName());
        Assert.Equal("platform", territoryType.GetSchema());
        Assert.NotNull(territoryType.FindProperty(nameof(Territory.Id)));
        Assert.NotNull(territoryType.FindProperty(nameof(Territory.TenantId)));
        Assert.NotNull(territoryType.FindProperty(nameof(Territory.Code)));
        Assert.NotNull(territoryType.FindProperty(nameof(Territory.Name)));
        Assert.NotNull(territoryType.FindProperty(nameof(Territory.ParentTerritoryId)));
        Assert.NotNull(territoryType.FindProperty(nameof(Territory.Status)));

        // Territory Alternate Key (TenantId, Id)
        var territoryAk = territoryType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Territory.TenantId), nameof(Territory.Id) }));
        Assert.NotNull(territoryAk);

        // TenantUser entity verification
        var tenantUserType = model.FindEntityType(typeof(TenantUser));
        Assert.NotNull(tenantUserType);
        Assert.Equal("TenantUsers", tenantUserType.GetTableName());
        Assert.Equal("platform", tenantUserType.GetSchema());
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.Id)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.TenantId)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.ExternalIssuer)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.ExternalSubject)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.DisplayName)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.NormalizedEmail)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.DefaultBranchId)));
        Assert.NotNull(tenantUserType.FindProperty(nameof(TenantUser.Status)));

        // TenantUser Alternate Key (TenantId, Id)
        var userAk = tenantUserType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(TenantUser.TenantId), nameof(TenantUser.Id) }));
        Assert.NotNull(userAk);

        // Unique index on (TenantId, ExternalIssuer, ExternalSubject)
        var uniqueIdentityIndex = tenantUserType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(TenantUser.TenantId), nameof(TenantUser.ExternalIssuer), nameof(TenantUser.ExternalSubject) }));
        Assert.NotNull(uniqueIdentityIndex);

        // Branch FK from TenantUser (TenantId, DefaultBranchId) -> Branches (TenantId, Id)
        var branchFk = tenantUserType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Branch));
        Assert.NotNull(branchFk);
        Assert.Equal(DeleteBehavior.Restrict, branchFk.DeleteBehavior);
    }
}
