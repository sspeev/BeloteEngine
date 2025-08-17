using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs
{
    public class BeloteHub(
        ILogger<BeloteHub> _logger,
        ILobbyService _lobbyService) : Hub
    {
        private readonly ILogger<BeloteHub> logger = _logger;
        private readonly ILobbyService lobbyService = _lobbyService;

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

        public async Task JoinLobby(int lobbyId, string playerName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Lobby_{lobbyId}");
            // Keep this lightweight; controllers will broadcast full PlayersUpdated
            await Clients.Group($"Lobby_{lobbyId}").SendAsync("PlayerConnected", playerName);
        }

        public async Task LeaveLobby(int lobbyId, string playerName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Lobby_{lobbyId}");
            await Clients.Group($"Lobby_{lobbyId}").SendAsync("PlayerDisconnected", playerName);
        }
    }
}