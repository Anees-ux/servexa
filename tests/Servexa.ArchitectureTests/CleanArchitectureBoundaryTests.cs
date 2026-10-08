using System.Reflection;
using System.Xml.Linq;

namespace Servexa.ArchitectureTests;

/// <summary>
/// Enforces Clean Architecture dependency boundaries across the Servexa solution.
///
/// Boundaries enforced:
/// - Domain         -> No Servexa project dependencies, no infrastructure/web dependencies (PackageReference or AssemblyRef)
/// - Application    -> Domain only
/// - Infrastructure -> Application and Domain only
/// - API            -> Application and Infrastructure only
///
/// Note on Architecture Enforcement Scope:
/// - Direct Project-Reference & Package-Reference checks verify MSBuild project file definitions to prevent invalid compile-time references.
/// - Compiled Assembly-Reference checks inspect metadata references emitted into compiled assemblies (Assembly.GetReferencedAssemblies()).
/// - Reflection inspects compiled assembly references; it does not claim to inspect every internal namespace or type-level coupling if the compiler omits unused references.
/// </summary>
public class CleanArchitectureBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Servexa.Domain.Platform.Entities.Tenant).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Servexa.Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Servexa.Infrastructure.Persistence.ServexaDbContext).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Servexa.Api.Infrastructure.GlobalExceptionHandler).Assembly;

    #region Compiled Assembly Reference Checks

    [Fact]
    public void Domain_Assembly_ShouldNotReferenceAnyOtherServexaAssembly()
    {
        // Act
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert: Domain must not reference any other Servexa assembly
        var forbiddenServexaAssemblies = new[]
        {
            "Servexa.Application",
            "Servexa.Infrastructure",
            "Servexa.Api"
        };

        var violations = referencedAssemblies.Intersect(forbiddenServexaAssemblies).ToList();
        Assert.Empty(violations);
    }

    [Fact]
    public void Domain_Assembly_ShouldNotReferenceInfrastructureOrWebFrameworks()
    {
        // Act: Inspect compiled assembly metadata references
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert: Domain must not depend on EF Core or ASP.NET Core assemblies
        var violations = referencedAssemblies
            .Where(a => a.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                     || a.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_Assembly_ShouldNotReferenceInfrastructureOrApi()
    {
        // Act: Inspect compiled metadata references emitted by Roslyn compiler
        var referencedAssemblies = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert: Application must never reference Infrastructure or API
        var forbiddenServexaAssemblies = new[]
        {
            "Servexa.Infrastructure",
            "Servexa.Api"
        };

        var violations = referencedAssemblies.Intersect(forbiddenServexaAssemblies).ToList();
        Assert.Empty(violations);

        // Any Servexa assembly reference that is emitted must only be Servexa.Domain
        var servexaReferences = referencedAssemblies
            .Where(a => a.StartsWith("Servexa.", StringComparison.Ordinal))
            .ToList();

        Assert.All(servexaReferences, r => Assert.Equal("Servexa.Domain", r));
    }

    [Fact]
    public void Infrastructure_Assembly_ShouldNotReferenceApiAssembly()
    {
        // Act
        var referencedAssemblies = InfrastructureAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert: Infrastructure must not reference API
        Assert.DoesNotContain("Servexa.Api", referencedAssemblies);

        // Infrastructure may reference Domain
        Assert.Contains("Servexa.Domain", referencedAssemblies);
    }

    [Fact]
    public void Api_Assembly_ShouldReferenceApplicationAndInfrastructure_AmongServexaAssemblies()
    {
        // Act
        var referencedAssemblies = ApiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert: API references Application and Infrastructure
        Assert.Contains("Servexa.Application", referencedAssemblies);
        Assert.Contains("Servexa.Infrastructure", referencedAssemblies);
    }

    #endregion

    #region Direct Project & Package Reference (.csproj) Checks

    [Fact]
    public void Domain_Project_ShouldHaveNoServexaProjectReferences()
    {
        var csprojPath = Path.Combine(GetSolutionRoot(), "src", "Servexa.Domain", "Servexa.Domain.csproj");
        var references = GetDirectProjectReferences(csprojPath);

        var forbidden = new[] { "Servexa.Application", "Servexa.Infrastructure", "Servexa.Api" };
        var violations = references.Intersect(forbidden).ToList();

        Assert.Empty(violations);
        Assert.Empty(references);
    }

    [Fact]
    public void Domain_Project_ShouldNotDeclareForbiddenPackageReferences()
    {
        var csprojPath = Path.Combine(GetSolutionRoot(), "src", "Servexa.Domain", "Servexa.Domain.csproj");
        var packageReferences = GetPackageReferences(csprojPath);

        // Domain must never declare package references to EF Core or ASP.NET Core
        var violations = packageReferences
            .Where(p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                     || p.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_Project_ShouldOnlyReferenceDomainProject()
    {
        var csprojPath = Path.Combine(GetSolutionRoot(), "src", "Servexa.Application", "Servexa.Application.csproj");
        var references = GetDirectProjectReferences(csprojPath);

        var forbidden = new[] { "Servexa.Infrastructure", "Servexa.Api" };
        var violations = references.Intersect(forbidden).ToList();

        Assert.Empty(violations);
        Assert.Single(references);
        Assert.Contains("Servexa.Domain", references);
    }

    [Fact]
    public void Infrastructure_Project_ShouldOnlyReferenceDomainAndApplicationProjects()
    {
        var csprojPath = Path.Combine(GetSolutionRoot(), "src", "Servexa.Infrastructure", "Servexa.Infrastructure.csproj");
        var references = GetDirectProjectReferences(csprojPath);

        var forbidden = new[] { "Servexa.Api" };
        var violations = references.Intersect(forbidden).ToList();

        Assert.Empty(violations);
        Assert.Contains("Servexa.Domain", references);
        Assert.Contains("Servexa.Application", references);
        Assert.DoesNotContain("Servexa.Api", references);
    }

    [Fact]
    public void Api_Project_ShouldOnlyReferenceApplicationAndInfrastructureProjects()
    {
        var csprojPath = Path.Combine(GetSolutionRoot(), "src", "Servexa.Api", "Servexa.Api.csproj");
        var references = GetDirectProjectReferences(csprojPath);

        // API is composition root referencing Application and Infrastructure
        var forbidden = new[] { "Servexa.Domain" };
        var violations = references.Intersect(forbidden).ToList();

        Assert.Empty(violations);
        Assert.Contains("Servexa.Application", references);
        Assert.Contains("Servexa.Infrastructure", references);
    }

    #endregion

    #region Controlled Negative Tests

    [Fact]
    public void ControlledNegativeTest_AssemblyReferenceValidator_DetectsForbiddenDependency()
    {
        // Arrange: Simulate Application having an illegal reference to Servexa.Infrastructure
        var simulatedReferences = new List<string>
        {
            "System.Runtime",
            "Servexa.Domain",
            "Servexa.Infrastructure" // Forbidden
        };

        var forbiddenAssemblies = new[] { "Servexa.Infrastructure", "Servexa.Api" };

        // Act
        var violations = simulatedReferences.Intersect(forbiddenAssemblies).ToList();

        // Assert: Confirms that the boundary rule successfully flags the forbidden reference
        Assert.NotEmpty(violations);
        Assert.Contains("Servexa.Infrastructure", violations);
    }

    [Fact]
    public void ControlledNegativeTest_AssemblyReferenceValidator_DetectsForbiddenFrameworkAssemblies()
    {
        // Arrange: Simulate Domain assembly having an illegal reference to EF Core or ASP.NET Core
        var simulatedReferences = new List<string>
        {
            "System.Runtime",
            "Microsoft.EntityFrameworkCore.SqlServer", // Forbidden
            "Microsoft.AspNetCore.Mvc"                 // Forbidden
        };

        // Act
        var violations = simulatedReferences
            .Where(a => a.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                     || a.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert: Confirms that framework violations are caught
        Assert.Equal(2, violations.Count);
        Assert.Contains("Microsoft.EntityFrameworkCore.SqlServer", violations);
        Assert.Contains("Microsoft.AspNetCore.Mvc", violations);
    }

    [Fact]
    public void ControlledNegativeTest_ProjectReferenceValidator_DetectsForbiddenProjectReference()
    {
        // Arrange: Simulate a project file containing a forbidden ProjectReference
        var simulatedCsprojXml = @"
            <Project Sdk=""Microsoft.NET.Sdk"">
              <ItemGroup>
                <ProjectReference Include=""..\Servexa.Domain\Servexa.Domain.csproj"" />
                <ProjectReference Include=""..\Servexa.Infrastructure\Servexa.Infrastructure.csproj"" />
              </ItemGroup>
            </Project>";

        // Act
        var references = ParseProjectReferencesFromXml(simulatedCsprojXml);
        var forbidden = new[] { "Servexa.Infrastructure", "Servexa.Api" };
        var violations = references.Intersect(forbidden).ToList();

        // Assert: Confirms that the project reference parser successfully detects the violation
        Assert.NotEmpty(violations);
        Assert.Contains("Servexa.Infrastructure", violations);
    }

    [Fact]
    public void ControlledNegativeTest_PackageReferenceValidator_DetectsForbiddenFrameworkPackage()
    {
        // Arrange: Simulate a Domain project file declaring forbidden EF Core & ASP.NET Core packages
        var simulatedCsprojXml = @"
            <Project Sdk=""Microsoft.NET.Sdk"">
              <ItemGroup>
                <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""10.0.0"" />
                <PackageReference Include=""Microsoft.AspNetCore.App"" />
              </ItemGroup>
            </Project>";

        // Act
        var packageReferences = ParsePackageReferencesFromXml(simulatedCsprojXml);
        var violations = packageReferences
            .Where(p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                     || p.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert: Confirms both forbidden packages are caught
        Assert.Equal(2, violations.Count);
        Assert.Contains("Microsoft.EntityFrameworkCore", violations);
        Assert.Contains("Microsoft.AspNetCore.App", violations);
    }

    [Theory]
    [InlineData(@"..\Servexa.Domain\Servexa.Domain.csproj", "Servexa.Domain")]
    [InlineData("../Servexa.Domain/Servexa.Domain.csproj", "Servexa.Domain")]
    [InlineData(@"src\Servexa.Infrastructure\Servexa.Infrastructure.csproj", "Servexa.Infrastructure")]
    [InlineData("src/Servexa.Infrastructure/Servexa.Infrastructure.csproj", "Servexa.Infrastructure")]
    public void ParseProjectReferencesFromXml_ShouldNormalizeWindowsAndLinuxSeparators(string includePath, string expectedProjectName)
    {
        var xml = $@"
            <Project Sdk=""Microsoft.NET.Sdk"">
              <ItemGroup>
                <ProjectReference Include=""{includePath}"" />
              </ItemGroup>
            </Project>";

        var references = ParseProjectReferencesFromXml(xml);

        Assert.Single(references);
        Assert.Equal(expectedProjectName, references[0]);
    }

    #endregion

    #region Helpers

    public static List<string> GetDirectProjectReferences(string csprojFilePath)
    {
        if (!File.Exists(csprojFilePath))
        {
            throw new FileNotFoundException($"Project file not found: {csprojFilePath}");
        }

        var xml = File.ReadAllText(csprojFilePath);
        return ParseProjectReferencesFromXml(xml);
    }

    public static List<string> ParseProjectReferencesFromXml(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        return doc.Descendants("ProjectReference")
            .Select(pr => (string?)pr.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include =>
            {
                var normalized = include!.Replace('\\', '/');
                var fileName = Path.GetFileName(normalized);
                return Path.GetFileNameWithoutExtension(fileName);
            })
            .ToList();
    }

    public static List<string> GetPackageReferences(string csprojFilePath)
    {
        if (!File.Exists(csprojFilePath))
        {
            throw new FileNotFoundException($"Project file not found: {csprojFilePath}");
        }

        var xml = File.ReadAllText(csprojFilePath);
        return ParsePackageReferencesFromXml(xml);
    }

    public static List<string> ParsePackageReferencesFromXml(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        return doc.Descendants("PackageReference")
            .Select(pr => (string?)pr.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .ToList();
    }

    private static string GetSolutionRoot()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "Servexa.sln")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
        {
            throw new InvalidOperationException("Could not locate solution root containing Servexa.sln.");
        }

        return currentDir.FullName;
    }

    #endregion
}
