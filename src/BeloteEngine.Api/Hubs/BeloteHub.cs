using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs
{
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

        public async Task JoinLobby(int lobbyId, RequestInfoModel request)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Lobby_{lobbyId}");
            var player = new Player { Name = request.PlayerName, LobbyId = request.LobbyId };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
                throw new HubException(joinResult.ErrorMessage);
        }

        public async Task<DeleteModel> LeaveLobby(LeaveRequestModel request)
        {
            var player = new Player 
            { 
                Name = request.PlayerName, 
                LobbyId = request.LobbyId 
            };

            Lobby lobbyBeforeLeave = lobbyService.GetLobby(request.LobbyId);
            bool isLeavingPlayerHost = lobbyBeforeLeave.ConnectedPlayers.Any(p =>
                p != null &&
                p.Name == request.PlayerName &&
                p.Hoster);

            bool success = lobbyService.LeaveLobby(player, request.LobbyId);

            if (success)
            {
                var lobby = lobbyService.GetLobby(request.LobbyId);
                if (isLeavingPlayerHost && (lobby == null || lobby.ConnectedPlayers.Count == 0))
                {
                    await Clients.Group($"Lobby_{request.LobbyId}")
                        .DeleteLobby();

                    return new DeleteModel
                    {
                        IsLeaveSuccessfull = true,
                        IsDeletingSuccessfull = true
                    };
                }

                // Normal case: player left but lobby continues
                if (lobby != null)
                {
                    // Send the complete lobby object with updated ConnectedPlayers
                    //await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                    //    .SendAsync("PlayerLeft", lobby);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Lobby_{request.LobbyId}");
                }
            }
            return new DeleteModel
            {
                IsLeaveSuccessfull = success,
                IsDeletingSuccessfull = null
            };
        }

        public async Task DeleteLobby(Lobby lobby, LeaveRequestModel request)
        {
            // Notify all clients that the lobby is closing
            //await Clients.Group($"Lobby_{request.LobbyId}")
            //    .SendAsync("LobbyDeleted", new
            //    {
            //        LobbyId = request.LobbyId,
            //        Reason = "Host left the lobby"
            //    });

            if (lobby != null)
            {
                lobbyService.ResetLobby(lobby.Id);
            }
        }

        public async Task StartGame(int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            await Clients.Group($"Lobby_{lobbyId}").StartGame(lobby);
        }
        
        //public async Task DealingCards(int lobbyId, Queue<Player> players)
        //{
        //    var dealer =  gameService.PlayerToDealCards(players);
        //    await Clients.Group($"Lobby_{lobbyId}").SendAsync("DealCards", dealer);
        //}
    }
}
