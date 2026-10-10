using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Servexa.Api.Controllers;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Security;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Platform.Enums;
using Servexa.Infrastructure.Identity;
using Servexa.Infrastructure.Persistence;

namespace Servexa.ArchitectureTests;

public class SecurityAndAuthorizationFoundationTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();

    [Fact]
    public void JwtTokenGenerator_MissingSecretKey_ShouldFailFastAndThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "https://identity.servexa.local",
            ["Jwt:Audience"] = "servexa-api"
        }).Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(config));
        Assert.Contains("Jwt:SecretKey", exception.Message);
    }

    [Fact]
    public void JwtTokenGenerator_ShortSecretKey_ShouldFailFastAndThrowInvalidOperationException()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "too-short-key",
            ["Jwt:Issuer"] = "https://identity.servexa.local",
            ["Jwt:Audience"] = "servexa-api"
        }).Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(config));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void JwtTokenGenerator_MissingIssuerOrAudience_ShouldFailFast()
    {
        var configWithoutIssuer = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "SuperSecretKeyForServexaSecurityTestsOnly_32BytesMinimumLength!",
            ["Jwt:Audience"] = "servexa-api"
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(configWithoutIssuer));

        var configWithoutAudience = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "SuperSecretKeyForServexaSecurityTestsOnly_32BytesMinimumLength!",
            ["Jwt:Issuer"] = "https://identity.servexa.local"
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(configWithoutAudience));
    }

    [Fact]
    public async Task TenantResolutionService_InactiveTenant_ShouldFailClosed()
    {
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ServexaDbContext(options);
        var tenant = new Tenant("SUSP", "Suspended Tenant", "Suspended", "UTC", "USD", id: TenantA);
        tenant.UpdateStatus(TenantStatus.Suspended);
        await dbContext.Tenants.AddAsync(tenant);
        await dbContext.SaveChangesAsync();

        var resolver = new TenantResolutionService(dbContext);
        var result = await resolver.ResolveTenantMembershipAsync(TenantA, UserA);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not permit operations", result.FailureReason);
    }

    [Fact]
    public async Task TenantResolutionService_InactiveTenantUser_ShouldFailClosed()
    {
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ServexaDbContext(options);
        var tenant = new Tenant("ACME", "Acme Corp", "Acme", "UTC", "USD", id: TenantA);
        var user = new TenantUser(
            tenantId: TenantA,
            externalIssuer: "https://identity.servexa.local",
            externalSubject: "sub-123",
            displayName: "Suspended User",
            normalizedEmail: "USER@ACME.COM",
            status: TenantUserStatus.Disabled,
            id: UserA);

        await dbContext.Tenants.AddAsync(tenant);
        await dbContext.TenantUsers.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var resolver = new TenantResolutionService(dbContext);
        var result = await resolver.ResolveTenantMembershipAsync(TenantA, UserA);

        Assert.False(result.IsSuccess);
        Assert.Contains("is not active", result.FailureReason);
    }

    [Fact]
    public async Task TenantResolutionService_CrossTenantIdentity_ShouldFailClosed()
    {
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ServexaDbContext(options);
        var tenantA = new Tenant("TENA", "Tenant A", "Tenant A", "UTC", "USD", id: TenantA);
        var tenantB = new Tenant("TENB", "Tenant B", "Tenant B", "UTC", "USD", id: TenantB);

        // User belongs ONLY to Tenant A
        var userA = new TenantUser(
            tenantId: TenantA,
            externalIssuer: "https://identity.servexa.local",
            externalSubject: "sub-alice",
            displayName: "Alice",
            normalizedEmail: "ALICE@TENA.COM",
            id: UserA);

        await dbContext.Tenants.AddRangeAsync(tenantA, tenantB);
        await dbContext.TenantUsers.AddAsync(userA);
        await dbContext.SaveChangesAsync();

        var resolver = new TenantResolutionService(dbContext);

        // Attempt to request Tenant B context with Alice's external identity
        var result = await resolver.ResolveExternalIdentityAsync(
            "https://identity.servexa.local",
            "sub-alice",
            requestedTenantId: TenantB);

        Assert.False(result.IsSuccess);
        Assert.Contains("No tenant membership found", result.FailureReason);
    }

    [Fact]
    public async Task TenantResolutionService_AuthoritativelyResolvesPermissionsFromDatabase()
    {
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ServexaDbContext(options);
        var tenant = new Tenant("ACME", "Acme Corp", "Acme", "UTC", "USD", id: TenantA);
        var user = new TenantUser(
            tenantId: TenantA,
            externalIssuer: "https://identity.servexa.local",
            externalSubject: "sub-123",
            displayName: "Valid User",
            normalizedEmail: "USER@ACME.COM",
            id: UserA);

        var perm1 = new Permission(Capabilities.CustomerView, "View customers");
        var perm2 = new Permission(Capabilities.CustomerCreate, "Create customers");
        var perm3 = new Permission(Capabilities.SiteView, "View sites");

        var role = new Role(TenantA, "Dispatcher");
        role.AddPermission(perm1.Id);
        role.AddPermission(perm3.Id);

        var assignment = new RoleAssignment(
            tenantId: TenantA,
            userId: UserA,
            roleId: role.Id,
            effectiveFromUtc: DateTime.UtcNow.AddDays(-10));

        await dbContext.Tenants.AddAsync(tenant);
        await dbContext.TenantUsers.AddAsync(user);
        await dbContext.Permissions.AddRangeAsync(perm1, perm2, perm3);
        await dbContext.Roles.AddAsync(role);
        await dbContext.RoleAssignments.AddAsync(assignment);
        await dbContext.SaveChangesAsync();

        var resolver = new TenantResolutionService(dbContext);
        var result = await resolver.ResolveTenantMembershipAsync(TenantA, UserA);

        Assert.True(result.IsSuccess);
        Assert.Contains("Dispatcher", result.Roles);
        Assert.Contains(Capabilities.CustomerView, result.Permissions);
        Assert.Contains(Capabilities.SiteView, result.Permissions);
        Assert.DoesNotContain(Capabilities.CustomerCreate, result.Permissions); // Not granted
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldEnforceAuthoritativePermissions()
    {
        var fakeContext = new FakeSecurityTenantContext(
            tenantId: TenantA,
            userId: UserA,
            isAuthenticated: true,
            permissions: [Capabilities.CustomerView]);

        var handler = new PermissionAuthorizationHandler(fakeContext);

        // Test granted requirement
        var grantedRequirement = new PermissionRequirement(Capabilities.CustomerView);
        var grantedAuthContext = new AuthorizationHandlerContext(
            [grantedRequirement],
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "123")], "Bearer")),
            resource: null);

        await handler.HandleAsync(grantedAuthContext);
        Assert.True(grantedAuthContext.HasSucceeded);

        // Test denied requirement
        var deniedRequirement = new PermissionRequirement(Capabilities.CustomerCreate);
        var deniedAuthContext = new AuthorizationHandlerContext(
            [deniedRequirement],
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "123")], "Bearer")),
            resource: null);

        await handler.HandleAsync(deniedAuthContext);
        Assert.False(deniedAuthContext.HasSucceeded);

        // Test pipe-separated OR requirement (e.g. WorkOrderCreate || TechnicianExecute)
        var orRequirement = new PermissionRequirement($"{Capabilities.WorkOrderCreate}|{Capabilities.TechnicianExecute}");

        // Context with WorkOrderCreate
        var managerContext = new FakeSecurityTenantContext(TenantA, UserA, isAuthenticated: true, [Capabilities.WorkOrderCreate]);
        var managerHandler = new PermissionAuthorizationHandler(managerContext);
        var managerAuthContext = new AuthorizationHandlerContext([orRequirement], new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "123")], "Bearer")), null);
        await managerHandler.HandleAsync(managerAuthContext);
        Assert.True(managerAuthContext.HasSucceeded);

        // Context with TechnicianExecute
        var techContext = new FakeSecurityTenantContext(TenantA, UserA, isAuthenticated: true, [Capabilities.TechnicianExecute]);
        var techHandler = new PermissionAuthorizationHandler(techContext);
        var techAuthContext = new AuthorizationHandlerContext([orRequirement], new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "123")], "Bearer")), null);
        await techHandler.HandleAsync(techAuthContext);
        Assert.True(techAuthContext.HasSucceeded);

        // Context with only CustomerView (neither WorkOrderCreate nor TechnicianExecute)
        var unauthorizedContext = new FakeSecurityTenantContext(TenantA, UserA, isAuthenticated: true, [Capabilities.CustomerView]);
        var unauthorizedHandler = new PermissionAuthorizationHandler(unauthorizedContext);
        var unauthorizedAuthContext = new AuthorizationHandlerContext([orRequirement], new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "123")], "Bearer")), null);
        await unauthorizedHandler.HandleAsync(unauthorizedAuthContext);
        Assert.False(unauthorizedAuthContext.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationPolicyProvider_DynamicallyCreatesPolicyWithPermissionRequirement()
    {
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionAuthorizationPolicyProvider(options);

        var policy = await provider.GetPolicyAsync(Capabilities.CustomerCreate);

        Assert.NotNull(policy);
        var requirement = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());
        Assert.Equal(Capabilities.CustomerCreate, requirement.Permission);
    }

    [Fact]
    public async Task DevelopmentAuthController_OutsideDevelopment_MustReturnNotFound()
    {
        var prodEnv = new FakeWebHostEnvironment("Production");
        var fakeSeeder = new FakeDevDataSeeder();
        var fakeResolver = new FakeTenantResolutionService();
        var fakeJwt = new FakeJwtTokenGenerator();

        var controller = new DevelopmentAuthController(
            prodEnv,
            fakeSeeder,
            fakeResolver,
            fakeJwt);

        var result = await controller.IssueDevelopmentToken(new DevelopmentAuthController.DevTokenRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void Controllers_MustEnforceApprovedCapabilityPermissions()
    {
        // AccountsController
        var createAccountAttr = typeof(AccountsController)
            .GetMethod(nameof(AccountsController.CreateAccount))!
            .GetCustomAttributes(typeof(HasPermissionAttribute), false)
            .Cast<HasPermissionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(createAccountAttr);
        Assert.Equal(Capabilities.CustomerCreate, createAccountAttr.Permission);

        var getAccountsAttr = typeof(AccountsController)
            .GetMethod(nameof(AccountsController.GetAccounts))!
            .GetCustomAttributes(typeof(HasPermissionAttribute), false)
            .Cast<HasPermissionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(getAccountsAttr);
        Assert.Equal(Capabilities.CustomerView, getAccountsAttr.Permission);

        // SitesController
        var createSiteAttr = typeof(SitesController)
            .GetMethod(nameof(SitesController.CreateSite))!
            .GetCustomAttributes(typeof(HasPermissionAttribute), false)
            .Cast<HasPermissionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(createSiteAttr);
        Assert.Equal(Capabilities.SiteCreate, createSiteAttr.Permission);

        var getSitesAttr = typeof(SitesController)
            .GetMethod(nameof(SitesController.GetSites))!
            .GetCustomAttributes(typeof(HasPermissionAttribute), false)
            .Cast<HasPermissionAttribute>()
            .SingleOrDefault();

        // WorkOrdersController
        var updateTaskStatusAttr = typeof(WorkOrdersController)
            .GetMethod(nameof(WorkOrdersController.UpdateWorkTaskStatus))!
            .GetCustomAttributes(typeof(HasPermissionAttribute), false)
            .Cast<HasPermissionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(updateTaskStatusAttr);
        Assert.Equal($"{Capabilities.WorkOrderCreate}|{Capabilities.TechnicianExecute}", updateTaskStatusAttr.Permission);

        var updateScopeItemStatusAttr = typeof(WorkOrdersController)
            .GetMethod(nameof(WorkOrdersController.UpdateScopeItemStatus))!
            .GetCustomAttributes(typeof(HasPermissionAttribute), false)
            .Cast<HasPermissionAttribute>()
            .SingleOrDefault();

        Assert.NotNull(updateScopeItemStatusAttr);
        Assert.Equal($"{Capabilities.WorkOrderCreate}|{Capabilities.TechnicianExecute}", updateScopeItemStatusAttr.Permission);
    }

    #region Test Fakes

    private sealed class FakeWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Servexa.Api";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class FakeDevDataSeeder : IDevDataSeeder
    {
        public Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeTenantResolutionService : ITenantResolutionService
    {
        public Task<TenantResolutionResult> ResolveTenantMembershipAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new TenantResolutionResult(true, tenantId, userId, "user@servexa.local", "User", ["Admin"], [Capabilities.CustomerView]));

        public Task<TenantResolutionResult> ResolveExternalIdentityAsync(string externalIssuer, string externalSubject, Guid? requestedTenantId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new TenantResolutionResult(true, Guid.NewGuid(), Guid.NewGuid(), "user@servexa.local", "User", ["Admin"], [Capabilities.CustomerView]));
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public string GenerateToken(Guid tenantId, Guid userId, string email, string displayName, IEnumerable<string> roles, IEnumerable<string>? permissions = null)
            => "fake.jwt.token";
    }

    private sealed class FakeSecurityTenantContext(
        Guid tenantId,
        Guid? userId,
        bool isAuthenticated,
        IReadOnlyList<string> permissions) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public Guid? UserId => userId;
        public string? UserEmail => "test@servexa.local";
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => ["User"];
        public IReadOnlyList<string> Permissions => permissions;
        public bool IsAuthenticated => isAuthenticated;
        public bool HasPermission(string permission) => permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    #endregion
}
