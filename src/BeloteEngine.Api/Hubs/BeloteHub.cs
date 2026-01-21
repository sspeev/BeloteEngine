using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs;

// Client -> Server
[Authorize]
public class BeloteHub(
      ILogger<BeloteHub> logger
    , ILobbyService lobbyService
    , IGameService gameService
    ) : Hub<IBeloteClient>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Player connected: {ConnectionId}", Context.ConnectionId);

        // Optional convenience: auto-join group if lobbyId is provided as a query param
        var http = Context.GetHttpContext();
        if (http?.Request.Query.TryGetValue("lobbyId", out var vals) == true &&
            int.TryParse(vals[0], out var lobbyId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Lobby_{lobbyId}");
            logger.LogInformation("Connection {ConnectionId} joined group Lobby_{LobbyId} via query param",
                Context.ConnectionId, lobbyId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }


    /// <summary>
    /// Adds the calling player to the specified lobby and notifies all lobby members of the new participant.
    /// </summary>
    /// <remarks>After joining, the player is added to the SignalR group for the lobby, and all
    /// members are notified of the new participant. The caller receives an updated lobby state.</remarks>
    /// <param name="request">An object containing the lobby identifier and the player's name. The lobby must exist, and the player name
    /// must be valid.</param>
    /// <returns>A task that represents the asynchronous join operation.</returns>
    /// <exception cref="HubException">Thrown if the specified lobby does not exist or if the player cannot join the lobby.</exception>
    public async Task JoinLobby(RequestInfoModel request)
    {
        Lobby lobby = lobbyService.GetLobby(request.LobbyId)
            ?? throw new HubException($"Lobby {request.LobbyId} not found");

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

        await Clients.Group($"Lobby_{request.LobbyId}")
            .PlayerJoined(request.LobbyId, request.PlayerName);

        var updatedLobby = lobbyService.GetLobby(request.LobbyId);
        await Clients.Caller.LobbyUpdated(updatedLobby);
    }

    /// <summary>
    /// Removes the specified player from the lobby and notifies all connected clients of the player's departure or
    /// lobby deletion.
    /// </summary>
    /// <remarks>If the player leaving is the host and no players remain in the lobby, the lobby is
    /// deleted and all clients in the lobby are notified. Otherwise, all clients are notified that the player has
    /// left.</remarks>
    /// <param name="request">The request containing the lobby identifier and the name of the player to remove from the lobby. Cannot be
    /// null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HubException">Thrown if the specified lobby does not exist or if the player cannot be removed from the lobby.</exception>
    public async Task LeaveLobby(LeaveRequestModel request)
    {
        Lobby lobbyBeforeLeave = lobbyService.GetLobby(request.LobbyId)
            ?? throw new HubException($"Lobby {request.LobbyId} not found");

        var player = new Player 
        { 
            Name = request.PlayerName, 
            LobbyId = request.LobbyId 
        };
        bool isHost = lobbyBeforeLeave.ConnectedPlayers.Any(p =>
            p.Name == request.PlayerName && p.Hoster);
            
        bool success = lobbyService.LeaveLobby(player, request.LobbyId);

        if(!success)
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
                .PlayerLeft(request.LobbyId, request.PlayerName);
        }
    }

    /// <summary>
    /// Starts a new game session in the specified lobby and notifies all connected clients in that lobby.
    /// </summary>
    /// <remarks>This method initializes the game state for the lobby and sends a notification to all
    /// clients in the lobby group. The game will not start if the lobby is not found.</remarks>
    /// <param name="lobbyId">The unique identifier of the lobby in which to start the game. Must correspond to an existing lobby.</param>
    /// <returns>A task that represents the asynchronous operation of starting the game and notifying clients.</returns>
    /// <exception cref="HubException">Thrown if the lobby with the specified lobbyId does not exist.</exception>
    public async Task StartGame(int lobbyId)
    {
        var lobby = lobbyService.GetLobby(lobbyId)
            ?? throw new HubException($"Lobby {lobbyId} not found");
        gameService.GameInitializer(lobby);
        gameService.InitialPhase(lobby);
        logger.LogInformation("Game started in lobby {LobbyId}", lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").GameStarted(lobby);
    }

    /// <summary>
    /// Deals cards to all players in the specified lobby and notifies clients of the updated game state.
    /// </summary>
    /// <remarks>This method sets the game phase to "dealing" and broadcasts the updated state to all
    /// clients in the lobby group. All connected players must be present for the operation to succeed.</remarks>
    /// <param name="lobbyId">The unique identifier of the lobby in which cards are to be dealt.</param>
    /// <param name="players">A queue containing the players who will receive cards. The order of players in the queue determines the
    /// dealing sequence.</param>
    /// <returns>A task that represents the asynchronous operation of dealing cards and notifying clients.</returns>
    /// <exception cref="HubException">Thrown if fewer than four players are connected to the lobby when dealing begins.</exception>
    public async Task DealingCards(int lobbyId, Queue<Player> players)
    {
        var lobby = lobbyService.GetLobby(lobbyId);
        if(lobby.ConnectedPlayers.Count < 4)
            throw new HubException("Someone left the lobby");

        lobby.GamePhase = "dealing";
        var dealer = gameService.PlayerToDealCards(players);
        foreach (var player in players)
        {
            gameService.GetPlayerCards(player.Name, lobby);
        }
        await Clients.Group($"Lobby_{lobbyId}").CardsDealt(lobbyId, lobby.GamePhase, dealer.Name);
        await Clients.Groups($"Lobby_{lobbyId}").LobbyUpdated(lobby);
    }

    /// <summary>
    /// Processes a bid from a player in the specified lobby and notifies all lobby participants of the updated state.
    /// </summary>
    /// <param name="lobbyId">The unique identifier of the lobby in which the bid is being placed.</param>
    /// <param name="playerName">The name of the player making the bid. Cannot be null or empty.</param>
    /// <param name="bid">The bid value submitted by the player. The format and validity of the bid are determined by the game rules.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HubException">Thrown if a lobby with the specified lobbyId does not exist.</exception>
    public async Task MakeBid(int lobbyId, string playerName, string bid)
    {
        var lobby = lobbyService.GetLobby(lobbyId)
            ?? throw new HubException($"Lobby {lobbyId} not found");

        gameService.MakeBid(playerName, bid, lobby);
        logger.LogInformation("Player {PlayerName} made bid {Bid} in lobby {LobbyId}",
            playerName, bid, lobbyId);
        await Clients.Group($"Lobby_{lobbyId}").LobbyUpdated(lobby);
    }

    //public async Task PlayCard(int lobbyId, string playerName, string card)
    //{
    //    var lobby = lobbyService.GetLobby(lobbyId);
    //    if (lobby == null)
    //        throw new HubException($"Lobby {lobbyId} not found");
    //    gameService.ProcessPlayCard(lobby, playerName, card);
    //    logger.LogInformation("Player {PlayerName} played card {Card} in lobby {LobbyId}",
    //        playerName, card, lobbyId);
    //    await Clients.Group($"Lobby_{lobbyId}").LobbyUpdated(lobby);
    //}
}
