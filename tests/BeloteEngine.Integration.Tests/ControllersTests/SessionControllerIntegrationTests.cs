using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BeloteEngine.Integration.Tests.ControllersTests;

public sealed class SessionControllerIntegrationTests
{
    [Fact]
    public async Task CreateSession_ShouldReturnNoContent_AndSetSessionCookie()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.PostAsync("/api/session", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));
        Assert.Contains(setCookieValues, value => value.Contains("belote_session=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SetSession_ShouldReturnOk_WithSanitizedPlayerName()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        await client.PostAsync("/api/session", content: null);

        // Act
        using var response = await client.PatchAsync("/api/session/Alice", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Alice", doc.RootElement.GetProperty("playerName").GetString());
    }

    [Fact]
    public async Task SetSession_ShouldReturnValidationProblem_WhenNameContainsOnlySpecialCharacters()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.PatchAsync("/api/session/%21%40%23%24", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSession_ShouldReturnNoContent()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        await client.PostAsync("/api/session", content: null);

        // Act
        using var response = await client.DeleteAsync("/api/session");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
