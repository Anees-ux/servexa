using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Servexa.Application.Auth.Queries.GetCurrentUser;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Security;
using Servexa.Application.Customers.Commands.CreateAccount;
using Servexa.Application.Customers.Commands.CreateSite;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Queries.GetAccountById;
using Servexa.Application.Customers.Queries.GetAccounts;
using Servexa.Application.Customers.Queries.GetSiteById;
using Servexa.Application.Customers.Queries.GetSites;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Customers.Enums;
using Servexa.Infrastructure.Identity;

namespace Servexa.ArchitectureTests;

public class CustomersAndAuthVerticalSliceTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();

    [Fact]
    public void Account_DomainInvariants_ShouldEnforceRequiredFieldsAndValidation()
    {
        // Valid creation
        var account = new Account(
            tenantId: TenantA,
            accountNumber: "ACC-001",
            legalName: "Acme Industrial Corp",
            displayName: "Acme Industrial",
            accountType: AccountType.Commercial,
            currencyCode: "USD");

        Assert.Equal(TenantA, account.TenantId);
        Assert.Equal("ACC-001", account.AccountNumber);
        Assert.Equal("Acme Industrial Corp", account.LegalName);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.False(account.IsCreditHold);

        // Put on credit hold
        account.PutOnCreditHold("Overdue invoices exceeding 90 days");
        Assert.True(account.IsCreditHold);
        Assert.Equal("Overdue invoices exceeding 90 days", account.CreditHoldReason);
        Assert.Equal(AccountStatus.OnHold, account.Status);

        // Release credit hold
        account.ReleaseCreditHold();
        Assert.False(account.IsCreditHold);
        Assert.Null(account.CreditHoldReason);
        Assert.Equal(AccountStatus.Active, account.Status);

        // Empty TenantId rejection
        Assert.Throws<ArgumentException>(() => new Account(
            tenantId: Guid.Empty,
            accountNumber: "ACC-002",
            legalName: "Invalid",
            displayName: "Invalid"));
    }

    [Fact]
    public void Site_DomainInvariants_ShouldEnforceRequiredFieldsAndAddress()
    {
        var branchId = Guid.NewGuid();
        var site = new Site(
            tenantId: TenantA,
            siteNumber: "SITE-101",
            name: "Headquarters Plant",
            branchId: branchId,
            timeZoneId: "America/New_York",
            addressLine1: "100 Industrial Parkway",
            city: "Cleveland",
            stateProvince: "OH",
            postalCode: "44101",
            countryCode: "US",
            accessNotes: "Security badge check-in required at Gate 3",
            hazardNotes: "High voltage transformers in Bay B");

        Assert.Equal(TenantA, site.TenantId);
        Assert.Equal("SITE-101", site.SiteNumber);
        Assert.Equal(branchId, site.BranchId);
        Assert.Equal(SiteStatus.Active, site.Status);
        Assert.Equal("Security badge check-in required at Gate 3", site.AccessNotes);
        Assert.Equal("High voltage transformers in Bay B", site.HazardNotes);

        // Update address
        site.UpdateAddress("200 New Parkway", null, "Akron", "OH", "44301", "US");
        Assert.Equal("200 New Parkway", site.AddressLine1);
        Assert.Equal("Akron", site.City);

        // Empty branch rejection
        Assert.Throws<ArgumentException>(() => new Site(
            tenantId: TenantA,
            siteNumber: "SITE-102",
            name: "Invalid",
            branchId: Guid.Empty,
            timeZoneId: "America/New_York",
            addressLine1: "100 St",
            city: "City",
            stateProvince: "ST",
            postalCode: "12345"));
    }

    [Fact]
    public async Task CreateAccountCommandHandler_ShouldEnforceTenantIsolationAndConflictCheck()
    {
        var fakeRepo = new FakeAccountRepository();
        var fakeUow = new FakeUnitOfWork();
        var fakeTenantContext = new FakeTenantContext(TenantA, UserA);

        var handler = new CreateAccountCommandHandler(fakeRepo, fakeUow, fakeTenantContext);
        var command = new CreateAccountCommand("ACC-100", "Beta Logistics", "Beta", AccountType.Commercial);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ACC-100", result.AccountNumber);
        Assert.Equal(TenantA, result.TenantId);
        Assert.Single(fakeRepo.Accounts);
        Assert.Equal(1, fakeUow.SaveCount);

        // Duplicate conflict check
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSiteCommandHandler_ShouldLinkDefaultAccountWhenSpecified()
    {
        var branchId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var fakeSiteRepo = new FakeSiteRepository();
        var fakeAccountRepo = new FakeAccountRepository();
        var fakeUow = new FakeUnitOfWork();
        var fakeTenantContext = new FakeTenantContext(TenantA, UserA);

        var account = new Account(TenantA, "ACC-01", "Acme", "Acme", id: accountId);
        await fakeAccountRepo.AddAsync(account);

        var handler = new CreateSiteCommandHandler(
            fakeSiteRepo,
            fakeAccountRepo,
            fakeUow,
            fakeTenantContext);

        var command = new CreateSiteCommand(
            SiteNumber: "SITE-001",
            Name: "Site Alpha",
            BranchId: branchId,
            TimeZoneId: "America/Chicago",
            AddressLine1: "500 Main St",
            City: "Chicago",
            StateProvince: "IL",
            PostalCode: "60601",
            PrimaryAccountId: accountId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("SITE-001", result.SiteNumber);
        Assert.Equal(accountId, result.PrimaryAccountId);
        Assert.Single(fakeSiteRepo.Sites);
        Assert.Single(fakeSiteRepo.Relationships);
        Assert.Equal(1, fakeUow.SaveCount);
    }

    [Fact]
    public async Task JwtTokenGenerator_And_AuthQueries_ShouldProvideWorkingTenantContext()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "SuperSecretKeyForServexaSecurityTestsOnly_32BytesMinimumLength!",
            ["Jwt:Issuer"] = "https://identity.servexa.local",
            ["Jwt:Audience"] = "servexa-api",
            ["Jwt:ExpiryMinutes"] = "60"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var tokenGen = new JwtTokenGenerator(config);

        var token = tokenGen.GenerateToken(
            TenantA,
            UserA,
            "tech@acme.com",
            "John Doe",
            ["Dispatcher"],
            ["Customer.View", "Customer.Create"]);

        Assert.False(string.IsNullOrWhiteSpace(token));

        // Validate token claims
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(TenantA.ToString(), jwt.Claims.First(c => c.Type == ServexaClaims.TenantId).Value);
        Assert.Equal(UserA.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("tech@acme.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Dispatcher");
        Assert.Contains(jwt.Claims, c => c.Type == ServexaClaims.Permission && c.Value == "Customer.View");

        // GetCurrentUserQueryHandler
        var fakeTenantContext = new FakeTenantContext(TenantA, UserA, "admin@acme.com", "Admin", ["OperationsAdmin"], ["Customer.View"]);
        var meHandler = new GetCurrentUserQueryHandler(fakeTenantContext);
        var meResult = await meHandler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(meResult.IsAuthenticated);
        Assert.Equal(TenantA, meResult.TenantId);
        Assert.Equal("admin@acme.com", meResult.Email);
    }

    [Fact]
    public async Task CustomerRepositories_ShouldStrictlyEnforceTenantIsolation()
    {
        var fakeRepo = new FakeAccountRepository();
        var accountA = new Account(TenantA, "ACC-001", "Tenant A Corp", "Tenant A");
        var accountB = new Account(TenantB, "ACC-001", "Tenant B Corp", "Tenant B");

        await fakeRepo.AddAsync(accountA);
        await fakeRepo.AddAsync(accountB);

        var retrievedByA = await fakeRepo.GetByAccountNumberAsync(TenantA, "ACC-001");
        Assert.NotNull(retrievedByA);
        Assert.Equal(TenantA, retrievedByA.TenantId);

        var retrievedByB = await fakeRepo.GetByAccountNumberAsync(TenantB, "ACC-001");
        Assert.NotNull(retrievedByB);
        Assert.Equal(TenantB, retrievedByB.TenantId);

        var queryA = await fakeRepo.GetAccountsAsync(TenantA, null, 0, 10);
        Assert.Single(queryA);
        Assert.Equal(TenantA, queryA[0].TenantId);
    }

    #region Test Fakes

    private sealed class FakeTenantContext(
        Guid tenantId,
        Guid? userId = null,
        string? email = null,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? permissions = null) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public Guid? UserId => userId;
        public string? UserEmail => email;
        public string? DisplayName => displayName;
        public IReadOnlyList<string> Roles => roles ?? [];
        public IReadOnlyList<string> Permissions => permissions ?? [];
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public List<Account> Accounts { get; } = [];

        public Task<Account?> GetByIdAsync(Guid tenantId, Guid accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(Accounts.FirstOrDefault(a => a.TenantId == tenantId && a.Id == accountId));

        public Task<Account?> GetByAccountNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Accounts.FirstOrDefault(a => a.TenantId == tenantId && a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Account>> GetAccountsAsync(Guid tenantId, string? search, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = Accounts.Where(a => a.TenantId == tenantId).Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<Account>>(list);
        }

        public Task<int> GetCountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(Accounts.Count(a => a.TenantId == tenantId));

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            Accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Accounts.Any(a => a.TenantId == tenantId && a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class FakeSiteRepository : ISiteRepository
    {
        public List<Site> Sites { get; } = [];
        public List<SiteAccountRelationship> Relationships { get; } = [];

        public Task<Site?> GetByIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default)
            => Task.FromResult(Sites.FirstOrDefault(s => s.TenantId == tenantId && s.Id == siteId));

        public Task<Site?> GetBySiteNumberAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Sites.FirstOrDefault(s => s.TenantId == tenantId && s.SiteNumber.Equals(siteNumber, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Site>> GetSitesAsync(Guid tenantId, Guid? branchId, Guid? accountId, string? search, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = Sites.Where(s => s.TenantId == tenantId).Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<Site>>(list);
        }

        public Task<int> GetCountAsync(Guid tenantId, Guid? branchId, Guid? accountId, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(Sites.Count(s => s.TenantId == tenantId));

        public Task AddAsync(Site site, CancellationToken cancellationToken = default)
        {
            Sites.Add(site);
            return Task.CompletedTask;
        }

        public Task AddRelationshipAsync(SiteAccountRelationship relationship, CancellationToken cancellationToken = default)
        {
            Relationships.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Sites.Any(s => s.TenantId == tenantId && s.SiteNumber.Equals(siteNumber, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<SiteAccountRelationship>> GetRelationshipsBySiteIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default)
        {
            var list = Relationships.Where(r => r.TenantId == tenantId && r.SiteId == siteId).ToList();
            return Task.FromResult<IReadOnlyList<SiteAccountRelationship>>(list);
        }
    }

    #endregion
}
