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
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            logger.LogInformation("Player disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinLobbyGroup(int lobbyId, string playerName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Lobby_{lobbyId}");
            await Clients.Group($"Lobby_{lobbyId}").SendAsync("PlayerConnected", playerName);
        }

        public async Task LeaveLobbyGroup(int lobbyId, string playerName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Lobby_{lobbyId}");
            await Clients.Group($"Lobby_{lobbyId}").SendAsync("PlayerDisconnected", playerName);
        }
    }
}
