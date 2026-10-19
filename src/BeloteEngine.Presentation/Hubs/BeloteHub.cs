using System.IdentityModel.Tokens.Jwt;
using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Application.DTOs;
using BeloteEngine.Application.Security;
using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Presentation.Hubs;

[EnableRateLimiting("fixed")]
[Authorize]
public class BeloteHub(
      ILogger<BeloteHub> logger
    , ILobbyService lobbyService
    , IGameService gameService
    , IAfkTimerService afkTimer
    , IHostEnvironment environment
    , IConnectionRegistry connectionRegistry
    ) : Hub<IBeloteClient>
{
    private const string UserIdItemKey = "BeloteHub.UserId";
    private const string LobbyIdItemKey = "BeloteHub.LobbyId";

    private readonly bool logSensitiveDetails = environment.IsDevelopment();

    public override async Task OnConnectedAsync()
    {
        var userId = GetAuthenticatedUserId();
        connectionRegistry.Register(userId, Context.ConnectionId);
        afkTimer.Register(Context.ConnectionId, Context);

        if (logSensitiveDetails)
            logger.LogInformation("Player connected: {ConnectionId}",
                Context.ConnectionId);
        else
            logger.LogInformation("Player connected");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        afkTimer.Unregister(Context.ConnectionId);

        var userId = GetConnectionUserId();
        var lobbyId = GetConnectionLobbyId();
        if (userId is not null && lobbyId is not null)
        {
            var lobby = lobbyService.GetLobby(lobbyId.Value);
            var player = lobby?.ConnectedPlayers.FirstOrDefault(p =>
                string.Equals(p.UserId, userId, StringComparison.OrdinalIgnoreCase));

            if (player is not null && lobbyService.LeaveLobby(player, lobbyId.Value))
            {
                var updatedLobby = lobbyService.GetLobby(lobbyId.Value);
                if (updatedLobby is null)
                    await Clients.All.LobbyDeleted(lobbyId.Value);
                else
                    await Clients.Group($"Lobby_{lobbyId.Value}").PlayerLeft(updatedLobby);

                logger.LogInformation("Player {PlayerName} removed from lobby {LobbyId} on disconnect",
                    player.Name, lobbyId.Value);
            }
        }

        if (logSensitiveDetails)
            logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);
        else
            logger.LogInformation("Player disconnected");

        await base.OnDisconnectedAsync(exception);
    }

    // ── Lobby ─────────────────────────────────────────────────────────────────

    private string GetAuthenticatedUserId()
    {
        return Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new HubException("Authentication is required.");
    }

    public async Task<Lobby> JoinLobby(JoinModel request)
    {
        if (request is null)
            throw new HubException("Join request is required.");

        try
        {
            request.PlayerName = InputValidator.SanitizePlayerName(request.PlayerName);
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }

        var userId = GetAuthenticatedUserId();

        Player player = new()
        {
            UserId = userId,
            Name = request.PlayerName,
            LobbyId = request.LobbyId,
        };

        var joinResult = lobbyService.JoinLobby(request.LobbyId, player);
        if (!joinResult.Success)
            throw new HubException(joinResult.ErrorMessage);

        Context.Items[UserIdItemKey] = player.UserId;
        Context.Items[LobbyIdItemKey] = request.LobbyId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Lobby_{request.LobbyId}");
        logger.LogInformation("Player {PlayerName} joined lobby {LobbyId}", request.PlayerName, request.LobbyId);

        var updatedLobby = lobbyService.GetLobby(request.LobbyId);
        await Clients.Group($"Lobby_{request.LobbyId}").PlayerJoined(updatedLobby);

        return updatedLobby;
    }

    public async Task LeaveLobby(LeaveRequestModel request)
    {
        var lobby = GetLobbyOrThrow(request.LobbyId);
        var callingPlayer = GetCallerOrThrow(lobby);

        if (callingPlayer.Name != request.PlayerName)
        {
            if (logSensitiveDetails)
                logger.LogWarning("Player {ActualName} tried to leave as {FakeName}",
                    callingPlayer.Name, request.PlayerName);
            else
                logger.LogWarning("Player leave identity mismatch blocked");
            throw new HubException("You can only leave as yourself");
        }

        bool success = lobbyService.LeaveLobby(callingPlayer, request.LobbyId);

        if (!success)
            throw new HubException("Failed to leave the lobby.");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Lobby_{request.LobbyId}");
        logger.LogInformation("Player {PlayerName} left lobby {LobbyId}", request.PlayerName, request.LobbyId);

        var updatedLobby = lobbyService.GetLobby(request.LobbyId);

        if (updatedLobby == null || updatedLobby.ConnectedPlayers.Count == 0)
        {
            await Clients.Group($"Lobby_{request.LobbyId}").LobbyDeleted(request.LobbyId);
            // Also notify everyone so the lobby list updates immediately
            await Clients.All.LobbyDeleted(request.LobbyId);
            logger.LogInformation("Lobby {LobbyId} deleted", request.LobbyId);
        }
        else
        {
            await Clients.Group($"Lobby_{request.LobbyId}").PlayerLeft(updatedLobby);
        }
    }

    // ── Game setup ────────────────────────────────────────────────────────────

    public async Task StartGame(int lobbyId)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        GetCallerOrThrow(lobby); // just validate presence
        gameService.GameInitializer(lobby);
        gameService.InitialPhase(lobby);
        lobby.UpdateActivity();
        logger.LogInformation("Game started in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").GameStarted(lobby);
    }

    public async Task DealingCards(int lobbyId, List<Player> playersList)
    {
        var lobby = GetLobbyOrThrow(lobbyId);

        if (lobby.ConnectedPlayers.Count < 4)
            throw new HubException("Someone left the lobby");

        var players = lobby.Game.SortedPlayers;

        lobby.GamePhase = "dealing";
        lobby.UpdateActivity();

        var dealer = gameService.PlayerToDealCards(players);
        lobby.Game.CurrentPlayer = dealer;

        var firstBidder = gameService.PlayerToStartAnnounceAndPlay(players);
        lobby.Game.CurrentPlayer = firstBidder;

        foreach (var player in players)
            gameService.GetPlayerCards(player, lobby.Game.Deck);

        await Clients.Group($"Lobby_{lobbyId}").CardsDealt(lobby, dealer.Name, firstBidder.Name);

        afkTimer.Start(Context.ConnectionId);
    }

    public async Task SkipRound(int lobbyId)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        GetCallerOrThrow(lobby);
        gameService.GameReset(lobby);
        lobby.UpdateActivity();
        logger.LogInformation("Round skipped (no bid) in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").GameSkipped(lobby);
    }

    public async Task ResetGame(int lobbyId)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        GetCallerOrThrow(lobby);

        gameService.GameReset(lobby);
        lobby.UpdateActivity();

        logger.LogInformation("Game reset in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").GameRestarted(lobby);
    }

    // ── Bidding ───────────────────────────────────────────────────────────────

    public async Task MakeBid(int lobbyId, string playerName, string bid)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        lobby.GamePhase = "bidding";

        var callingPlayer = GetCallerOrThrow(lobby);
        ValidateTurn(callingPlayer, playerName, lobby, action: "bid");

        var nextPlayer = gameService.MakeBid(playerName, bid, lobby);
        lobby.UpdateActivity();

        logger.LogInformation("Player {PlayerName} made bid {Bid} in lobby {LobbyId}. Next: {NextPlayer}",
            playerName, bid, lobbyId, nextPlayer.Name);

        await Clients.Group($"Lobby_{lobbyId}").BidMade(lobby);

        afkTimer.Transfer(Context.ConnectionId, Context.ConnectionId);
    }

    // ── Gameplay ──────────────────────────────────────────────────────────────

    public async Task Gameplay(int lobbyId)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        var callingPlayer = GetCallerOrThrow(lobby);

        gameService.Gameplay(lobby);
        lobby.UpdateActivity();

        logger.LogInformation("Gameplay started in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").Gameplay(lobby);

        afkTimer.Start(Context.ConnectionId);
    }

    public async Task PlayCard(int lobbyId, string playerName, Card card)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        var callingPlayer = GetCallerOrThrow(lobby);
        ValidateTurn(callingPlayer, playerName, lobby, action: "play");

        PlayCardResult result;
        try
        {
            result = gameService.PlayCard(playerName, card, lobby);
        }
        catch (InvalidOperationException ex) { throw new HubException(ex.Message); }
        catch (ArgumentException ex) { throw new HubException(ex.Message); }

        if (result.GameOver)
        {
            lobby.GamePhase = "gameover";
        }

        lobby.UpdateActivity();
        logger.LogInformation(
            "Player {PlayerName} played {Card} in lobby {LobbyId}. TrickWinner={TrickWinner} RoundComplete={RoundComplete} GameOver={GameOver}",
            playerName, $"{card.Rank} of {card.Suit}", lobbyId,
            result.TrickWinner?.Name ?? "none", result.RoundComplete, result.GameOver);

        await Clients.Group($"Lobby_{lobbyId}").CardPlayed(lobby);

        // Don't start a new timer when the round/game just ended — DealingCards restarts the chain
        if (!result.RoundComplete && !result.GameOver)
            afkTimer.Transfer(Context.ConnectionId, Context.ConnectionId);
        else
            afkTimer.Cancel(Context.ConnectionId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Lobby GetLobbyOrThrow(int lobbyId)
        => lobbyService.GetLobby(lobbyId)
           ?? throw new HubException($"Lobby {lobbyId} not found");

    private Player GetCallerOrThrow(Lobby lobby)
    {
        var userId = GetConnectionUserId();
        return lobby.ConnectedPlayers.FirstOrDefault(p =>
                   userId is not null &&
                   string.Equals(p.UserId, userId, StringComparison.OrdinalIgnoreCase))
               ?? throw new HubException("You are not in this lobby");
    }

    private string? GetConnectionUserId()
        => Context.Items.TryGetValue(UserIdItemKey, out var value) ? value as string : null;

    private int? GetConnectionLobbyId()
        => Context.Items.TryGetValue(LobbyIdItemKey, out var value) && value is int lobbyId
            ? lobbyId
            : null;

    private void ValidateTurn(Player caller, string claimedName, Lobby lobby, string action)
    {
        if (caller.Name != claimedName)
        {
            if (logSensitiveDetails)
                logger.LogWarning("Player {ActualName} tried to {Action} as {FakeName}",
                    caller.Name, action, claimedName);
            else
                logger.LogWarning("Identity mismatch blocked for {Action}", action);
            throw new HubException($"You can only {action} for yourself");
        }

        if (lobby.Game.CurrentPlayer?.Name != claimedName)
        {
            if (logSensitiveDetails)
                logger.LogWarning("Player {PlayerName} tried to {Action} out of turn. Current: {CurrentPlayer}",
                    claimedName, action, lobby.Game.CurrentPlayer?.Name);
            else
                logger.LogWarning("Out-of-turn {Action} attempt blocked", action);
            throw new HubException($"It's not your turn to {action}");
        }
    }
}
