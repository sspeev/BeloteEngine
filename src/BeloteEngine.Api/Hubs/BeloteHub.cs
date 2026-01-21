using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs
{
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
            var lobby = lobbyService.GetLobby(lobbyId);
            if (lobby == null)
                throw new HubException($"Lobby {lobbyId} not found");

            gameService.GameInitializer(lobby);
            gameService.InitialPhase(lobby);
            logger.LogInformation("Game started in lobby {LobbyId}", lobbyId);
            await Clients.Group($"Lobby_{lobbyId}").GameStarted(lobby);
        }

        public async Task DealingCards(int lobbyId, Queue<Player> players)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            if(lobby.ConnectedPlayers.Count < 4)
                throw new HubException("Someone left the lobby");

            lobby.GamePhase = "dealing";
            var dealer = gameService.PlayerToDealCards(players);
            foreach (var player in players)
            {
                var playerCards = await gameService.GetPlayerCards(lobbyId, player.Name);
                await Clients.Client(player.ConnectionId)
                    .CardsDealt(playerCards);
            }
            await Clients.Group($"Lobby_{lobbyId}").CardsDealt(lobbyId, lobby.GamePhase);
            await Clients.Groups($"Lobby_{lobbyId}").LobbyUpdated(lobby);
        }

        public async Task MakeBid(int lobbyId, string playerName, string bid)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            if (lobby == null)
                throw new HubException($"Lobby {lobbyId} not found");
            gameService.ProcessBid(lobby, playerName, bid);
            logger.LogInformation("Player {PlayerName} made bid {Bid} in lobby {LobbyId}",
                playerName, bid, lobbyId);
            await Clients.Group($"Lobby_{lobbyId}").LobbyUpdated(lobby);
        }

        public async Task PlayCard(int lobbyId, string playerName, string card)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            if (lobby == null)
                throw new HubException($"Lobby {lobbyId} not found");
            gameService.ProcessPlayCard(lobby, playerName, card);
            logger.LogInformation("Player {PlayerName} played card {Card} in lobby {LobbyId}",
                playerName, card, lobbyId);
            await Clients.Group($"Lobby_{lobbyId}").LobbyUpdated(lobby);
        }
    }
}
