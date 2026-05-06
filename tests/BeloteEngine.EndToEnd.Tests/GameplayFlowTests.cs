using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace BeloteEngine.EndToEnd.Tests;

public sealed class GameplayFlowTests : EndToEndTestBase
{
    [Fact]
    public async Task GameplayFlow_ShouldPlayCard_AndNotifyAllPlayers()
    {
        // Arrange — full setup: lobby → join 4 → start → deal → bid → gameplay
        var lobbyId = await CreateLobbyAsync("Play Room");
        var connections = await SetupFullLobbyAsync(lobbyId);
        var names = new[] { "Player0", "Player1", "Player2", "Player3" };

        await connections[0].InvokeAsync("StartGame", lobbyId);
        await Task.Delay(200);

        // Deal and get first bidder
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

        // First bidder bids Clubs, then 3 passes to close the auction
        var bidderIndex = Array.FindIndex(names, n => n == currentBidder);
        await MakeBidAndWait(connections[bidderIndex], lobbyId, currentBidder!, "Clubs");

        for (int i = 1; i <= 3; i++)
        {
            var nextIndex = (bidderIndex + i) % 4;
            if (i < 3)
                await MakeBidAndWait(connections[nextIndex], lobbyId, names[nextIndex], "Pass");
            else
                await connections[nextIndex].InvokeAsync("MakeBid", lobbyId, names[nextIndex], "Pass");
        }
        await Task.Delay(200);

        // Trigger Gameplay phase
        var gameplayReceived = new TaskCompletionSource<object>();
        connections[0].On<object>("Gameplay", lobby => gameplayReceived.TrySetResult(lobby));

        await connections[0].InvokeAsync("Gameplay", lobbyId);
        await Task.WhenAny(gameplayReceived.Task, Task.Delay(5000));
        Assert.True(gameplayReceived.Task.IsCompletedSuccessfully, "Gameplay callback not received");

        // Parse lobby to find current player and their first card
        var lobbyJson = JsonSerializer.Serialize(await gameplayReceived.Task);
        using var doc = JsonDocument.Parse(lobbyJson);
        var currentPlayerName = doc.RootElement
            .GetProperty("game")
            .GetProperty("currentPlayer")
            .GetProperty("name")
            .GetString();
        Assert.NotNull(currentPlayerName);

        var playerIndex = Array.FindIndex(names, n => n == currentPlayerName);

        // Find the first card in the current player's hand
        JsonElement? firstCard = null;
        foreach (var p in doc.RootElement.GetProperty("game").GetProperty("sortedPlayers").EnumerateArray())
        {
            if (p.GetProperty("name").GetString() == currentPlayerName)
            {
                var hand = p.GetProperty("hand");
                if (hand.GetArrayLength() > 0)
                    firstCard = hand[0];
                break;
            }
        }
        Assert.NotNull(firstCard);

        var card = new
        {
            suit = firstCard.Value.GetProperty("suit").GetInt32(),
            rank = firstCard.Value.GetProperty("rank").GetString(),
            value = firstCard.Value.GetProperty("value").GetInt32(),
            power = firstCard.Value.GetProperty("power").GetInt32()
        };

        // Listen for CardPlayed on another player
        var otherIndex = (playerIndex + 1) % 4;
        var cardPlayedReceived = new TaskCompletionSource<bool>();
        connections[otherIndex].On<object>("CardPlayed", _ => cardPlayedReceived.TrySetResult(true));

        // Act
        await connections[playerIndex].InvokeAsync("PlayCard", lobbyId, currentPlayerName, card);
        var received = await Task.WhenAny(cardPlayedReceived.Task, Task.Delay(5000));

        // Assert
        Assert.True(received == cardPlayedReceived.Task, "CardPlayed callback not received");
    }

    [Fact]
    public async Task GameplayFlow_ShouldRejectOutOfTurnPlay()
    {
        // Arrange — set up to gameplay phase
        var lobbyId = await CreateLobbyAsync("OoT Play Room");
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

        var bidderIndex = Array.FindIndex(names, n => n == currentBidder);
        await MakeBidAndWait(connections[bidderIndex], lobbyId, currentBidder!, "Clubs");
        for (int i = 1; i <= 3; i++)
        {
            var nextIndex = (bidderIndex + i) % 4;
            if (i < 3)
                await MakeBidAndWait(connections[nextIndex], lobbyId, names[nextIndex], "Pass");
            else
                await connections[nextIndex].InvokeAsync("MakeBid", lobbyId, names[nextIndex], "Pass");
        }
        await Task.Delay(200);

        var gameplayReceived = new TaskCompletionSource<object>();
        connections[0].On<object>("Gameplay", lobby => gameplayReceived.TrySetResult(lobby));
        await connections[0].InvokeAsync("Gameplay", lobbyId);
        await Task.WhenAny(gameplayReceived.Task, Task.Delay(5000));

        var lobbyJson = JsonSerializer.Serialize(await gameplayReceived.Task);
        using var doc = JsonDocument.Parse(lobbyJson);
        var currentPlayerName = doc.RootElement
            .GetProperty("game")
            .GetProperty("currentPlayer")
            .GetProperty("name")
            .GetString();
        Assert.NotNull(currentPlayerName);

        // Find a wrong player
        var wrongIndex = Array.FindIndex(names, n => n != currentPlayerName);
        var wrongName = names[wrongIndex];
        var dummyCard = new { suit = 0, rank = "7", value = 0, power = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            () => connections[wrongIndex].InvokeAsync("PlayCard", lobbyId, wrongName, dummyCard));

        Assert.Contains("not your turn", exception.Message);
    }

    private static async Task MakeBidAndWait(
        HubConnection connection, int lobbyId, string playerName, string bid)
    {
        var bidMadeTcs = new TaskCompletionSource<bool>();
        connection.On<object>("BidMade", _ => bidMadeTcs.TrySetResult(true));
        await connection.InvokeAsync("MakeBid", lobbyId, playerName, bid);
        await Task.WhenAny(bidMadeTcs.Task, Task.Delay(3000));
    }
}
