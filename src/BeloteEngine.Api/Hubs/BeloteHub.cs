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
    ) : Hub<IBeloteClient>
{
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
        logger.LogInformation("Player connected: {ConnectionId} from {IpAddress}",
            Context.ConnectionId, ipAddress);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        connectionLimiter.RemoveConnection(ipAddress, Context.ConnectionId);
        logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

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
        await Clients.Group($"Lobby_{request.LobbyId}")
            .PlayerJoined(updatedLobby);
    }

    public async Task LeaveLobby(LeaveRequestModel request)
    {
        Lobby lobbyBeforeLeave = lobbyService.GetLobby(request.LobbyId)
            ?? throw new HubException($"Lobby {request.LobbyId} not found");

        // Validate caller owns this player
        var callingPlayer = lobbyBeforeLeave.ConnectedPlayers
            .FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

        if (callingPlayer == null)
            throw new HubException("You are not in this lobby");

        if (callingPlayer.Name != request.PlayerName)
        {
            logger.LogWarning("Player {ActualName} tried to leave as {FakeName}",
                callingPlayer.Name, request.PlayerName);
            throw new HubException("You can only leave as yourself");
        }

        var player = new Player
        {
            Name = request.PlayerName,
            LobbyId = request.LobbyId
        };

        bool isHost = callingPlayer.Hoster;
        bool success = lobbyService.LeaveLobby(player, request.LobbyId);

        if (!success)
            throw new HubException("Failed to leave the lobby.");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Lobby_{request.LobbyId}");
        logger.LogInformation("Player {PlayerName} left lobby {LobbyId}",
            request.PlayerName, request.LobbyId);

        var updatedLobby = lobbyService.GetLobby(request.LobbyId);
        bool shouldDelete = isHost &&
            (updatedLobby == null || updatedLobby.ConnectedPlayers.Count == 0);

        if (shouldDelete)
        {
            await Clients.Group($"Lobby_{request.LobbyId}").LobbyDeleted(request.LobbyId);
            logger.LogInformation("Lobby {LobbyId} deleted", request.LobbyId);
        }
        else
        {
            await Clients.Group($"Lobby_{request.LobbyId}")
                .PlayerLeft(updatedLobby!);
        }
    }

    public async Task StartGame(int lobbyId)
    {
        var lobby = lobbyService.GetLobby(lobbyId)
            ?? throw new HubException($"Lobby {lobbyId} not found");

        // Validate caller is in the lobby
        var callingPlayer = lobby.ConnectedPlayers
            .FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

        if (callingPlayer == null)
            throw new HubException("You are not in this lobby");

        gameService.GameInitializer(lobby);
        gameService.InitialPhase(lobby);
        lobby.UpdateActivity();

        logger.LogInformation("Game started in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").GameStarted(lobby);
    }

    public async Task DealingCards(int lobbyId, Queue<Player> players)
    {
        var lobby = lobbyService.GetLobby(lobbyId);
        if (lobby.ConnectedPlayers.Count < 4)
            throw new HubException("Someone left the lobby");

        lobby.GamePhase = "dealing";
        lobby.UpdateActivity();

        var dealer = gameService.PlayerToDealCards(players);
        foreach (var player in players)
        {
            gameService.GetPlayerCards(player.Name, lobby);
        }

        await Clients.Group($"Lobby_{lobbyId}").CardsDealt(lobby, dealer.Name);
    }

    public async Task MakeBid(int lobbyId, string playerName, string bid)
    {
        var lobby = lobbyService.GetLobby(lobbyId)
            ?? throw new HubException($"Lobby {lobbyId} not found");

        // Validate the caller owns this player
        var callingPlayer = lobby.ConnectedPlayers
            .FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

        if (callingPlayer == null)
            throw new HubException("You are not in this lobby");

        if (callingPlayer.Name != playerName)
        {
            logger.LogWarning("Player {ActualName} tried to bid as {FakeName}",
                callingPlayer.Name, playerName);
            throw new HubException("You can only make bids for yourself");
        }

        // Validate it's their turn
        if (lobby.Game?.CurrentPlayer?.Name != playerName)
            throw new HubException("It's not your turn");

        // Validate game phase
        if (lobby.GamePhase != "bidding")
            throw new HubException("Not in bidding phase");

        gameService.MakeBid(playerName, bid, lobby);
        lobby.UpdateActivity();

        logger.LogInformation("Player {PlayerName} made bid {Bid} in lobby {LobbyId}",
            playerName, bid, lobbyId);

        await Clients.Group($"Lobby_{lobbyId}").BidMade(lobby);
    }
}