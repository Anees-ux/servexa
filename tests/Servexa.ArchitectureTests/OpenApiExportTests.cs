using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Servexa.ArchitectureTests;

public class OpenApiExportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiExportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiSpec_ShouldBeValid_AndExportedToWebClient()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(json));

        // Validate JSON parsing
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("openapi", out _) || root.TryGetProperty("swagger", out _));
        Assert.True(root.TryGetProperty("paths", out var paths));

        // Verify key paths exist
        Assert.True(paths.TryGetProperty("/api/v1/bookings", out _));
        Assert.True(paths.TryGetProperty("/api/v1/resources", out _));
        Assert.True(paths.TryGetProperty("/api/v1/field/me/assignments", out _));
        Assert.True(paths.TryGetProperty("/api/v1/work-orders", out _));
        Assert.True(paths.TryGetProperty("/api/v1/assets", out _));

        // Export to web directory for npm openapi-typescript client code generation
        var currentDir = Directory.GetCurrentDirectory();
        // Traverse up to find repo root
        var dirInfo = new DirectoryInfo(currentDir);
        while (dirInfo != null && !Directory.Exists(Path.Combine(dirInfo.FullName, "web")))
        {
            dirInfo = dirInfo.Parent;
        }

        if (dirInfo != null)
        {
            var webOpenApiPath = Path.Combine(dirInfo.FullName, "web", "openapi.json");
            await File.WriteAllTextAsync(webOpenApiPath, json);
        }
    }
}
