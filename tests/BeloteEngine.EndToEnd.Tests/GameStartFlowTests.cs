using Microsoft.AspNetCore.SignalR.Client;

namespace BeloteEngine.EndToEnd.Tests;

public sealed class GameStartFlowTests : EndToEndTestBase
{
    [Fact]
    public async Task GameFlow_ShouldStartGame_WhenFourPlayersJoin()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Start Room");
        var connections = await SetupFullLobbyAsync(lobbyId);

        var gameStartedReceived = new TaskCompletionSource<bool>();
        connections[1].On<object>("GameStarted", _ => gameStartedReceived.TrySetResult(true));

        // Act
        await connections[0].InvokeAsync("StartGame", lobbyId);
        var received = await Task.WhenAny(gameStartedReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == gameStartedReceived.Task, "GameStarted callback not received");
    }

    [Fact]
    public async Task GameFlow_ShouldDealCards_AfterStarting()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Deal Room");
        var connections = await SetupFullLobbyAsync(lobbyId);

        await connections[0].InvokeAsync("StartGame", lobbyId);
        await Task.Delay(200);

        var cardsDealtReceived = new TaskCompletionSource<bool>();
        connections[2].On<object, string, string>("CardsDealt", (_, _, _) =>
            cardsDealtReceived.TrySetResult(true));

        // Act
        await connections[0].InvokeAsync("DealingCards", lobbyId, (object?)null);
        var received = await Task.WhenAny(cardsDealtReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == cardsDealtReceived.Task, "CardsDealt callback not received");
    }

    [Fact]
    public async Task GameFlow_ShouldRejectStartGame_WhenNotInLobby()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Outsider Room");
        var outsider = await CreateAuthenticatedPlayerAsync("Outsider");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => outsider.InvokeAsync("StartGame", lobbyId));

        Assert.Contains("not in this lobby", exception.Message);
    }
}
