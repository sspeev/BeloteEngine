using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Models;
using BeloteEngine.Services.Security;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs;

public class BeloteHub(
      ILogger<BeloteHub> logger
    , ILobbyService lobbyService
    , IGameService gameService
    , IConnectionLimiter connectionLimiter
    , IAfkTimerService afkTimer
    ) : Hub<IBeloteClient>
{
    // ── Hub lifecycle ─────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!connectionLimiter.CanConnect(ipAddress))
        {
            logger.LogWarning("Connection limit reached for IP {IpAddress}", ipAddress);
            Context.Abort();
            return;
        }

        connectionLimiter.TrackConnection(ipAddress, Context.ConnectionId);
        afkTimer.Register(Context.ConnectionId, Context);

        logger.LogInformation("Player connected: {ConnectionId} from {IpAddress}",
            Context.ConnectionId, ipAddress);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        connectionLimiter.RemoveConnection(ipAddress, Context.ConnectionId);
        afkTimer.Unregister(Context.ConnectionId);

        logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    // ── Lobby ─────────────────────────────────────────────────────────────────

    public async Task JoinLobby(JoinModel request)
    {
        try
        {
            request.PlayerName = InputValidator.SanitizePlayerName(request.PlayerName);
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }

        Player player = new()
        {
            Name = request.PlayerName,
            LobbyId = request.LobbyId,
            ConnectionId = Context.ConnectionId
        };

        var joinResult = lobbyService.JoinLobby(player);
        if (!joinResult.Success)
            throw new HubException(joinResult.ErrorMessage);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Lobby_{request.LobbyId}");
        logger.LogInformation("Player {PlayerName} joined lobby {LobbyId}", request.PlayerName, request.LobbyId);

        var updatedLobby = lobbyService.GetLobby(request.LobbyId);
        await Clients.Group($"Lobby_{request.LobbyId}").PlayerJoined(updatedLobby);
    }

    public async Task LeaveLobby(LeaveRequestModel request)
    {
        var lobby = GetLobbyOrThrow(request.LobbyId);
        var callingPlayer = GetCallerOrThrow(lobby);

        if (callingPlayer.Name != request.PlayerName)
        {
            logger.LogWarning("Player {ActualName} tried to leave as {FakeName}",
                callingPlayer.Name, request.PlayerName);
            throw new HubException("You can only leave as yourself");
        }

        var player = new Player { Name = request.PlayerName, LobbyId = request.LobbyId };

        bool isHost = callingPlayer.Hoster;
        bool success = lobbyService.LeaveLobby(player, request.LobbyId);

        if (!success)
            throw new HubException("Failed to leave the lobby.");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Lobby_{request.LobbyId}");
        logger.LogInformation("Player {PlayerName} left lobby {LobbyId}", request.PlayerName, request.LobbyId);

        var updatedLobby = lobbyService.GetLobby(request.LobbyId);
        bool shouldDelete = isHost && (updatedLobby == null || updatedLobby.ConnectedPlayers.Count == 0);

        if (shouldDelete)
        {
            await Clients.Group($"Lobby_{request.LobbyId}").LobbyDeleted(request.LobbyId);
            logger.LogInformation("Lobby {LobbyId} deleted", request.LobbyId);
        }
        else
        {
            await Clients.Group($"Lobby_{request.LobbyId}").PlayerLeft(updatedLobby!);
        }
    }

    // ── Game setup ────────────────────────────────────────────────────────────

    public async Task StartGame(int lobbyId)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        GetCallerOrThrow(lobby); // just validate presence

        gameService.GameInitializer(lobby);
        gameService.InitialPhase(lobby);
        lobby.Game.Splitter = lobby.Game.CurrentPlayer;
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
        lobby.Game.Dealer = dealer;

        var firstBidder = gameService.PlayerToStartAnnounceAndPlay(players);
        lobby.Game.Announcer = firstBidder;
        lobby.Game.CurrentPlayer = firstBidder;

        foreach (var player in players)
            gameService.GetPlayerCards(player, lobby.Game.Deck);

        await Clients.Group($"Lobby_{lobbyId}").CardsDealt(lobby, dealer.Name, firstBidder.Name);

        afkTimer.Start(firstBidder.ConnectionId);
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

        afkTimer.Transfer(Context.ConnectionId, nextPlayer.ConnectionId);
    }

    // ── Gameplay ──────────────────────────────────────────────────────────────

    public async Task Gameplay(int lobbyId)
    {
        var lobby = GetLobbyOrThrow(lobbyId);
        var callingPlayer = GetCallerOrThrow(lobby);

        if (lobby.Game.Starter == null)
            lobby.Game.Starter = callingPlayer;

        lobby.Game.CurrentPlayer = lobby.Game.Starter ?? callingPlayer;

        gameService.Gameplay(lobby);
        lobby.UpdateActivity();

        logger.LogInformation("Gameplay started in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").Gameplay(lobby);

        afkTimer.Start(lobby.Game.CurrentPlayer?.ConnectionId);
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
        catch (ArgumentException ex)         { throw new HubException(ex.Message); }

        lobby.UpdateActivity();
        logger.LogInformation(
            "Player {PlayerName} played {Card} in lobby {LobbyId}. TrickWinner={TrickWinner} RoundComplete={RoundComplete} GameOver={GameOver}",
            playerName, $"{card.Rank} of {card.Suit}", lobbyId,
            result.TrickWinner?.Name ?? "none", result.RoundComplete, result.GameOver);

        await Clients.Group($"Lobby_{lobbyId}").CardPlayed(lobby);

        // Don't start a new timer when the round/game just ended — DealingCards restarts the chain
        if (!result.RoundComplete && !result.GameOver)
            afkTimer.Transfer(Context.ConnectionId, lobby.Game.CurrentPlayer?.ConnectionId);
        else
            afkTimer.Cancel(Context.ConnectionId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Lobby GetLobbyOrThrow(int lobbyId)
        => lobbyService.GetLobby(lobbyId)
           ?? throw new HubException($"Lobby {lobbyId} not found");

    private Player GetCallerOrThrow(Lobby lobby)
        => lobby.ConnectedPlayers.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)
           ?? throw new HubException("You are not in this lobby");

    private void ValidateTurn(Player caller, string claimedName, Lobby lobby, string action)
    {
        if (caller.Name != claimedName)
        {
            logger.LogWarning("Player {ActualName} tried to {Action} as {FakeName}",
                caller.Name, action, claimedName);
            throw new HubException($"You can only {action} for yourself");
        }

        if (lobby.Game.CurrentPlayer?.Name != claimedName)
        {
            logger.LogWarning("Player {PlayerName} tried to {Action} out of turn. Current: {CurrentPlayer}",
                claimedName, action, lobby.Game.CurrentPlayer?.Name);
            throw new HubException($"It's not your turn to {action}");
        }
    }
}
