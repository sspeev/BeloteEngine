using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace BeloteEngine.EndToEnd.Tests;

public sealed class BiddingFlowTests : EndToEndTestBase
{
    [Fact]
    public async Task BiddingFlow_ShouldAcceptBid_AndNotifyAllPlayers()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Bid Room");
        var connections = await SetupFullLobbyAsync(lobbyId);
        var names = new[] { "Player0", "Player1", "Player2", "Player3" };

        await connections[0].InvokeAsync("StartGame", lobbyId);
        await Task.Delay(200);

        // Deal cards and capture the first bidder
        string? firstBidderName = null;
        var cardsDealtTcs = new TaskCompletionSource<bool>();
        connections[0].On<object, string, string>("CardsDealt", (_, _, bidderName) =>
        {
            firstBidderName = bidderName;
            cardsDealtTcs.TrySetResult(true);
        });
        await connections[0].InvokeAsync("DealingCards", lobbyId, (object?)null);
        await Task.WhenAny(cardsDealtTcs.Task, Task.Delay(5000));
        Assert.NotNull(firstBidderName);

        var bidderIndex = Array.FindIndex(names, n => n == firstBidderName);
        var otherIndex = (bidderIndex + 1) % 4;

        var bidMadeReceived = new TaskCompletionSource<bool>();
        connections[otherIndex].On<object>("BidMade", _ => bidMadeReceived.TrySetResult(true));

        // Act
        await connections[bidderIndex].InvokeAsync("MakeBid", lobbyId, firstBidderName, "Clubs");
        var received = await Task.WhenAny(bidMadeReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == bidMadeReceived.Task, "BidMade callback not received");
    }

    [Fact]
    public async Task BiddingFlow_ShouldRejectOutOfTurnBid()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("OoT Bid Room");
        var connections = await SetupFullLobbyAsync(lobbyId);
        var names = new[] { "Player0", "Player1", "Player2", "Player3" };

        await connections[0].InvokeAsync("StartGame", lobbyId);
        await Task.Delay(200);

        string? firstBidderName = null;
        var cardsDealtTcs = new TaskCompletionSource<bool>();
        connections[0].On<object, string, string>("CardsDealt", (_, _, bidderName) =>
        {
            firstBidderName = bidderName;
            cardsDealtTcs.TrySetResult(true);
        });
        await connections[0].InvokeAsync("DealingCards", lobbyId, (object?)null);
        await Task.WhenAny(cardsDealtTcs.Task, Task.Delay(5000));
        Assert.NotNull(firstBidderName);

        // Find a player who is NOT the first bidder
        var wrongIndex = Array.FindIndex(names, n => n != firstBidderName);
        var wrongName = names[wrongIndex];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connections[wrongIndex].InvokeAsync("MakeBid", lobbyId, wrongName, "Clubs"));

        Assert.Contains("not your turn", exception.Message);
    }

    [Fact]
    public async Task BiddingFlow_ShouldSkipRound_WhenAllPlayersPass()
    {
        // Arrange
        var lobbyId = await CreateLobbyAsync("Skip Room");
        var connections = await SetupFullLobbyAsync(lobbyId);
        var names = new[] { "Player0", "Player1", "Player2", "Player3" };

        await connections[0].InvokeAsync("StartGame", lobbyId);
        await Task.Delay(200);

        string? currentBidder = null;
        var cardsDealtTcs = new TaskCompletionSource<bool>();
        connections[0].On<object, string, string>("CardsDealt", (_, _, bidderName) =>
        {
            currentBidder = bidderName;
            cardsDealtTcs.TrySetResult(true);
        });
        await connections[0].InvokeAsync("DealingCards", lobbyId, (object?)null);
        await Task.WhenAny(cardsDealtTcs.Task, Task.Delay(5000));
        Assert.NotNull(currentBidder);

        // Act — All 4 players pass in turn order
        for (int i = 0; i < 4; i++)
        {
            var bidderIndex = Array.FindIndex(names, n => n == currentBidder);

            var bidMadeTcs = new TaskCompletionSource<object>();
            connections[bidderIndex].On<object>("BidMade", lobby =>
                bidMadeTcs.TrySetResult(lobby));

            await connections[bidderIndex].InvokeAsync("MakeBid", lobbyId, currentBidder, "Pass");

            if (i < 3)
            {
                await Task.WhenAny(bidMadeTcs.Task, Task.Delay(3000));
                if (bidMadeTcs.Task.IsCompletedSuccessfully)
                {
                    var lobbyJson = JsonSerializer.Serialize(bidMadeTcs.Task.Result);
                    using var doc = JsonDocument.Parse(lobbyJson);
                    currentBidder = doc.RootElement
                        .GetProperty("game")
                        .GetProperty("currentPlayer")
                        .GetProperty("name")
                        .GetString();
                }
            }
        }

        // Trigger SkipRound
        var gameSkippedReceived = new TaskCompletionSource<bool>();
        connections[1].On<object>("GameSkipped", _ => gameSkippedReceived.TrySetResult(true));

        await connections[0].InvokeAsync("SkipRound", lobbyId);
        var received = await Task.WhenAny(gameSkippedReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == gameSkippedReceived.Task, "GameSkipped callback not received");
    }
}
