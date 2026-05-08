using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BeloteEngine.Integration.Tests.ControllersTests;

public sealed class LobbyControllerIntegrationTests
{
    // ── CreateLobby ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLobby_ShouldReturn201_WithLobbyDetails()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = "Alice",
            LobbyName = "Test Room"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var lobby = doc.RootElement.GetProperty("lobby");
        Assert.True(lobby.GetProperty("id").GetInt32() > 0);
        Assert.Equal("Test Room", lobby.GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateLobby_ShouldReturnUnsupportedMediaType_WhenBodyIsMissing()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.PostAsync("/api/lobby/create", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task CreateLobby_ShouldReturnBadRequest_WhenLobbyNameIsEmpty()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = "Alice",
            LobbyName = ""
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLobby_ShouldReturnBadRequest_WhenSecondLobbyFromSameIp()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        await client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = "Alice",
            LobbyName = "First Room"
        });

        // Act
        using var response = await client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = "Bob",
            LobbyName = "Second Room"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("lobbies", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetAvailableLobbies ────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableLobbies_ShouldReturnOk_WithEmptyList_WhenNoLobbiesExist()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.GetAsync("/api/lobby/listLobbies");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var lobbies = doc.RootElement.GetProperty("lobbies");
        Assert.Equal(JsonValueKind.Array, lobbies.ValueKind);
        Assert.Equal(0, lobbies.GetArrayLength());
    }

    [Fact]
    public async Task GetAvailableLobbies_ShouldReturnLobbyList_AfterCreatingLobby()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        await client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = "Alice",
            LobbyName = "Visible Room"
        });

        // Act
        using var response = await client.GetAsync("/api/lobby/listLobbies");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var lobbies = doc.RootElement.GetProperty("lobbies");
        Assert.True(lobbies.GetArrayLength() >= 1);
        Assert.Equal("Visible Room", lobbies[0].GetProperty("name").GetString());
    }

    // ── GetLobby ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetLobby_ShouldReturnNotFound_WhenLobbyDoesNotExist()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        // Act
        using var response = await client.GetAsync("/api/lobby/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLobby_ShouldReturnOk_WithLobbyDetails_WhenLobbyExists()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var createResponse = await client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = "Alice",
            LobbyName = "Detail Room"
        });
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var lobbyId = createDoc.RootElement.GetProperty("lobby").GetProperty("id").GetInt32();

        // Act
        using var response = await client.GetAsync($"/api/lobby/{lobbyId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var lobby = doc.RootElement.GetProperty("lobby");
        Assert.Equal(lobbyId, lobby.GetProperty("id").GetInt32());
        Assert.Equal("Detail Room", lobby.GetProperty("name").GetString());
        Assert.Equal("waiting", lobby.GetProperty("gamePhase").GetString());
        Assert.False(lobby.GetProperty("gameStarted").GetBoolean());
    }

    // ── Helpers ─────────────────────────────────────────────────────────

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
