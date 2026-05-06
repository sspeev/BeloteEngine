using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace BeloteEngine.EndToEnd.Tests;

/// <summary>
/// Shared infrastructure for E2E tests. Provides factory, HTTP client,
/// and authenticated SignalR connection helpers.
/// Each test class instance gets its own factory and client for state isolation.
/// </summary>
public abstract class EndToEndTestBase : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<HubConnection> _connections = [];
    private readonly List<HttpClient> _playerClients = [];

    protected HttpClient Client { get; }

    protected EndToEndTestBase()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Creates a lobby via HTTP and returns its ID.
    /// </summary>
    protected async Task<int> CreateLobbyAsync(string lobbyName, string playerName = "Host")
    {
        using var response = await Client.PostAsJsonAsync("/api/lobby/create", new
        {
            PlayerName = playerName,
            LobbyName = lobbyName
        });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("lobby").GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Creates a session, binds a player name, then opens a SignalR HubConnection
    /// authenticated with that session cookie.
    /// </summary>
    protected async Task<HubConnection> CreateAuthenticatedPlayerAsync(string playerName)
    {
        var playerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _playerClients.Add(playerClient);

        // 1. Create session
        using var sessionResponse = await playerClient.PostAsync("/api/session", content: null);
        sessionResponse.EnsureSuccessStatusCode();

        // 2. Set player name on session
        using var setResponse = await playerClient.PatchAsync($"/api/session/{playerName}", content: null);
        setResponse.EnsureSuccessStatusCode();

        // 3. Extract the Set-Cookie header value from SetSession response
        // (the CreateSession cookie has no PlayerName yet and won't pass hub validation).
        var cookieHeader = ExtractSessionCookie(setResponse);

        // 4. Build HubConnection with the cookie
        var hubUrl = _factory.Server.BaseAddress + "beloteHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add("Cookie", cookieHeader);
            })
            .Build();

        await connection.StartAsync();
        _connections.Add(connection);
        return connection;
    }

    /// <summary>
    /// Joins a player into a lobby via the SignalR hub, waiting for the PlayerJoined callback.
    /// </summary>
    protected static async Task JoinLobbyAsync(HubConnection connection, string playerName, int lobbyId)
    {
        var tcs = new TaskCompletionSource<bool>();
        connection.On<object>("PlayerJoined", _ => tcs.TrySetResult(true));

        await connection.InvokeAsync("JoinLobby", new { PlayerName = playerName, LobbyId = lobbyId });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        if (completed != tcs.Task)
            throw new TimeoutException($"PlayerJoined callback not received for {playerName}");
    }

    /// <summary>
    /// Creates 4 authenticated players and joins them all to the specified lobby.
    /// Returns the connections in order [Player0, Player1, Player2, Player3].
    /// </summary>
    protected async Task<HubConnection[]> SetupFullLobbyAsync(int lobbyId)
    {
        var names = new[] { "Player0", "Player1", "Player2", "Player3" };
        var connections = new HubConnection[4];

        for (int i = 0; i < 4; i++)
        {
            connections[i] = await CreateAuthenticatedPlayerAsync(names[i]);
            await JoinLobbyAsync(connections[i], names[i], lobbyId);
        }

        return connections;
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var sessionCookie = cookies.FirstOrDefault(c =>
                c.Contains("belote_session=", StringComparison.OrdinalIgnoreCase));
            if (sessionCookie != null)
            {
                var cookieValue = sessionCookie.Split(';')[0];
                return cookieValue;
            }
        }

        throw new InvalidOperationException("No belote_session cookie found in responses");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _connections)
        {
            if (conn.State == HubConnectionState.Connected)
                await conn.StopAsync();
            await conn.DisposeAsync();
        }
        _connections.Clear();

        foreach (var playerClient in _playerClients)
        {
            playerClient.Dispose();
        }
        _playerClients.Clear();

        Client.Dispose();
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
