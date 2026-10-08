using Microsoft.EntityFrameworkCore;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Platform.Enums;
using Servexa.Infrastructure.Persistence;

namespace Servexa.ArchitectureTests;

public class AuthorizationDomainAndPersistenceTests
{
    #region Permission Tests

    [Fact]
    public void Permission_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var code = " booking.create ";
        var description = " Allows creating bookings ";

        // Act
        var permission = new Permission(code, description);

        // Assert
        Assert.NotEqual(Guid.Empty, permission.Id);
        Assert.Equal("booking.create", permission.Code);
        Assert.Equal("Allows creating bookings", permission.Description);
        Assert.Equal(PermissionStatus.Active, permission.Status);
        Assert.True(permission.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Equal(permission.CreatedAtUtc, permission.ModifiedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Permission_Creation_WithInvalidCode_ShouldThrowArgumentException(string? invalidCode)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Permission(invalidCode!, "Valid Description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Permission_Creation_WithInvalidDescription_ShouldThrowArgumentException(string? invalidDesc)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Permission("booking.create", invalidDesc!));
    }

    [Fact]
    public void Permission_UpdateDetails_ShouldUpdateDescriptionAndTimestamp()
    {
        // Arrange
        var permission = new Permission("booking.create", "Old Description");

        // Act
        permission.UpdateDetails(" New Description ");

        // Assert
        Assert.Equal("New Description", permission.Description);
    }

    [Fact]
    public void Permission_UpdateStatus_ShouldUpdateStatus()
    {
        // Arrange
        var permission = new Permission("booking.create", "Description");

        // Act
        permission.UpdateStatus(PermissionStatus.Inactive);

        // Assert
        Assert.Equal(PermissionStatus.Inactive, permission.Status);
    }

    #endregion

    #region Role Tests

    [Fact]
    public void Role_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var name = " Dispatcher ";

        // Act
        var role = new Role(tenantId, name);

        // Assert
        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal(tenantId, role.TenantId);
        Assert.Equal("Dispatcher", role.Name);
        Assert.Equal("DISPATCHER", role.NormalizedName);
        Assert.False(role.IsSystem);
        Assert.Equal(RoleStatus.Active, role.Status);
        Assert.Empty(role.Permissions);
        Assert.True(role.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Equal(role.CreatedAtUtc, role.ModifiedAtUtc);
    }

    [Fact]
    public void Role_Creation_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Role(Guid.Empty, "RoleName"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Role_Creation_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Role(Guid.NewGuid(), invalidName!));
    }

    [Fact]
    public void Role_AddAndRemovePermission_ShouldManageChildrenCorrectly()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Technician");
        var perm1 = Guid.NewGuid();
        var perm2 = Guid.NewGuid();

        // Act - Add permissions
        role.AddPermission(perm1);
        role.AddPermission(perm2);
        role.AddPermission(perm1); // Duplicate addition should be idempotent

        // Assert
        Assert.Equal(2, role.Permissions.Count);
        Assert.Contains(role.Permissions, p => p.PermissionId == perm1);
        Assert.Contains(role.Permissions, p => p.PermissionId == perm2);

        // Act - Remove permission
        role.RemovePermission(perm1);

        // Assert
        Assert.Single(role.Permissions);
        Assert.DoesNotContain(role.Permissions, p => p.PermissionId == perm1);
    }

    [Fact]
    public void Role_SystemRole_CannotBeDeactivated()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", isSystem: true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => role.UpdateStatus(RoleStatus.Inactive));
    }

    [Fact]
    public void Role_NonSystemRole_CanBeDeactivated()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "CustomRole", isSystem: false);

        // Act
        role.UpdateStatus(RoleStatus.Inactive);

        // Assert
        Assert.Equal(RoleStatus.Inactive, role.Status);
    }

    #endregion

    #region RoleAssignment & ScopeAssignment Tests

    [Fact]
    public void RoleAssignment_Creation_ShouldSetPropertiesAndInvariantsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var fromUtc = DateTime.UtcNow.AddDays(-1);
        var toUtc = DateTime.UtcNow.AddDays(30);

        // Act
        var assignment = new RoleAssignment(tenantId, userId, roleId, fromUtc, toUtc);

        // Assert
        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.Equal(tenantId, assignment.TenantId);
        Assert.Equal(userId, assignment.UserId);
        Assert.Equal(roleId, assignment.RoleId);
        Assert.Equal(fromUtc, assignment.EffectiveFromUtc);
        Assert.Equal(toUtc, assignment.EffectiveToUtc);
        Assert.Empty(assignment.Scopes);
        Assert.True(assignment.IsActive());
    }

    [Fact]
    public void RoleAssignment_Creation_WithInvalidDateWindow_ShouldThrowArgumentException()
    {
        // Arrange
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddDays(-1); // to is before from

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new RoleAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), fromUtc, toUtc));
    }

    [Fact]
    public void RoleAssignment_AddAndRemoveScope_ShouldManageScopesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var assignment = new RoleAssignment(tenantId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var branchId = Guid.NewGuid();

        // Act - Add scopes
        var scope1 = assignment.AddScope(ScopeType.Branch, branchId);
        var scope2 = assignment.AddScope(ScopeType.Tenant);
        var scopeDup = assignment.AddScope(ScopeType.Branch, branchId); // Duplicate should return existing

        // Assert
        Assert.Equal(2, assignment.Scopes.Count);
        Assert.Equal(scope1.Id, scopeDup.Id);
        Assert.Equal(tenantId, scope1.TenantId);
        Assert.Equal(assignment.Id, scope1.RoleAssignmentId);
        Assert.Equal(ScopeType.Branch, scope1.ScopeType);
        Assert.Equal(branchId, scope1.ScopeId);
        Assert.Null(scope2.ScopeId);

        // Act - Remove scope
        assignment.RemoveScope(scope1.Id);

        // Assert
        Assert.Single(assignment.Scopes);
        Assert.DoesNotContain(assignment.Scopes, s => s.Id == scope1.Id);
    }

    [Fact]
    public void RoleAssignment_IsActive_ShouldEvaluateDateWindowsCorrectly()
    {
        // Past assignment
        var past = new RoleAssignment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));
        Assert.False(past.IsActive());

        // Future assignment
        var future = new RoleAssignment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(30));
        Assert.False(future.IsActive());

        // Current assignment with open end
        var openEnded = new RoleAssignment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-5), null);
        Assert.True(openEnded.IsActive());
    }

    #endregion

    #region EF Core Persistence Model Metadata Tests

    [Fact]
    public void DbContext_Model_ShouldContainCorrectAuthorizationEntitiesAndMappings()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseSqlServer("Server=tcp:localhost,1433;Database=Servexa_Test;User Id=sa;Password=Test!;TrustServerCertificate=True")
            .Options;

        using var context = new ServexaDbContext(options);
        var model = context.Model;

        // 1. Role mapping
        var roleType = model.FindEntityType(typeof(Role));
        Assert.NotNull(roleType);
        Assert.Equal("Roles", roleType.GetTableName());
        Assert.Equal("platform", roleType.GetSchema());
        var roleAk = roleType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Role.TenantId), nameof(Role.Id) }));
        Assert.NotNull(roleAk);
        var roleUniqueName = roleType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Role.TenantId), nameof(Role.NormalizedName) }));
        Assert.NotNull(roleUniqueName);

        // 2. Permission mapping
        var permType = model.FindEntityType(typeof(Permission));
        Assert.NotNull(permType);
        Assert.Equal("Permissions", permType.GetTableName());
        Assert.Equal("platform", permType.GetSchema());
        var permUniqueCode = permType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Permission.Code) }));
        Assert.NotNull(permUniqueCode);

        // 3. RolePermission mapping
        var rpType = model.FindEntityType(typeof(RolePermission));
        Assert.NotNull(rpType);
        Assert.Equal("RolePermissions", rpType.GetTableName());
        Assert.Equal("platform", rpType.GetSchema());
        Assert.True(rpType.FindPrimaryKey()!.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId) }));

        // 4. RoleAssignment mapping
        var raType = model.FindEntityType(typeof(RoleAssignment));
        Assert.NotNull(raType);
        Assert.Equal("RoleAssignments", raType.GetTableName());
        Assert.Equal("platform", raType.GetSchema());
        var raAk = raType.GetKeys()
            .FirstOrDefault(k => !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RoleAssignment.TenantId), nameof(RoleAssignment.Id) }));
        Assert.NotNull(raAk);

        // Tenant-safe foreign keys on RoleAssignment
        var userFk = raType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TenantUser));
        Assert.NotNull(userFk);
        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);

        var roleFk = raType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Role));
        Assert.NotNull(roleFk);
        Assert.Equal(DeleteBehavior.Restrict, roleFk.DeleteBehavior);

        // 5. ScopeAssignment mapping
        var saType = model.FindEntityType(typeof(ScopeAssignment));
        Assert.NotNull(saType);
        Assert.Equal("ScopeAssignments", saType.GetTableName());
        Assert.Equal("platform", saType.GetSchema());

        var saFk = saType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(RoleAssignment));
        Assert.NotNull(saFk);
        Assert.Equal(DeleteBehavior.Cascade, saFk.DeleteBehavior);
    }

    #endregion
}
