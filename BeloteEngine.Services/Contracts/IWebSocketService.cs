using System.Net.WebSockets;

namespace BeloteEngine.Services.Contracts
{
    public interface IWebSocketService
    {
        Task HandleConnectionAsync(WebSocket webSocket, string playerId);
    }
}
