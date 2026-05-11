using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace BeloteEngine.EndToEnd.Tests;

public sealed class LobbyFlowTests : EndToEndTestBase
{
    [Fact]
    public async Task LobbyFlow_ShouldCreateLobbyAndAppearInAvailableList()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("E2E Room");

        // Act
        using var listResponse = await Client.GetAsync("/api/lobby/listLobbies");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var json = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var lobbies = doc.RootElement.GetProperty("lobbies");
        Assert.True(lobbies.GetArrayLength() >= 1);
        Assert.Contains(lobbies.EnumerateArray(),
            l => l.GetProperty("name").GetString() == "E2E Room");
    }

    [Fact]
    public async Task LobbyFlow_PlayerShouldJoinAndReceivePlayerJoinedCallback()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Join Room");
        var connection = await CreateAuthenticatedPlayerAsync("Alice");

        var playerJoinedReceived = new TaskCompletionSource<bool>();
        connection.On<object>("PlayerJoined", _ => playerJoinedReceived.TrySetResult(true));

        // Act
        await connection.InvokeAsync("JoinLobby", new { PlayerName = "Alice", LobbyId = lobbyId });
        var received = await Task.WhenAny(playerJoinedReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == playerJoinedReceived.Task, "PlayerJoined callback not received");
    }

    [Fact]
    public async Task LobbyFlow_PlayerShouldLeaveAndOthersReceivePlayerLeftCallback()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Leave Room");

        var alice = await CreateAuthenticatedPlayerAsync("Alice");
        await JoinLobbyAsync(alice, "Alice", lobbyId);

        var bob = await CreateAuthenticatedPlayerAsync("Bob");
        await JoinLobbyAsync(bob, "Bob", lobbyId);

        var playerLeftReceived = new TaskCompletionSource<bool>();
        alice.On<object>("PlayerLeft", _ => playerLeftReceived.TrySetResult(true));

        // Act
        await bob.InvokeAsync("LeaveLobby", new { PlayerName = "Bob", LobbyId = lobbyId });
        var received = await Task.WhenAny(playerLeftReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == playerLeftReceived.Task, "PlayerLeft callback not received by Alice");
    }

    [Fact]
    public async Task LobbyFlow_ShouldDisappearFromAvailableList_WhenFull()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Full Room");
        await SetupFullLobbyAsync(lobbyId);

        // Act
        using var listResponse = await Client.GetAsync("/api/lobby/listLobbies");
        var json = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var lobbies = doc.RootElement.GetProperty("lobbies");  // Assert — full lobby should not appear in available list
        var fullLobby = lobbies.EnumerateArray()
            .FirstOrDefault(l => l.GetProperty("name").GetString() == "Full Room");
        Assert.Equal(default, fullLobby);
    }

    [Fact]
    public async Task LobbyFlow_ShouldRejectJoin_WhenSessionIdentityMismatch()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Mismatch Room");
        var connection = await CreateAuthenticatedPlayerAsync("Alice");

        // Act — Try to join as "Bob" (mismatches the session cookie which has "Alice")
        var exception = await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => connection.InvokeAsync("JoinLobby", new { PlayerName = "Bob", LobbyId = lobbyId }));

        // Assert
        Assert.Contains("Session validation failed", exception.Message);
    }
}
