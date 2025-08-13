using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs
{
    [Authorize]
    public sealed class BeloteHub() : Hub
    {
        public async Task JoinLobby(int lobbyId, string playerName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId.ToString());
            await Clients.Group(lobbyId.ToString())
                .SendAsync("PlayerJoined", $"{playerName} joined in Lobby - {lobbyId}");
        
        
        }
    }
}
