using BeloteEngine.Services.Contracts;
using System.Net.WebSockets;
using System.Text;

namespace BeloteEngine.Services.Services
{
    public class WebSocketService : IWebSocketService
    {
        public async Task HandleConnectionAsync(WebSocket webSocket, string playerId)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // Handle the message
                        Console.WriteLine($"Received from {playerId}: {message}");

                        // Echo back
                        var responseMessage = $"Echo: {message}";
                        var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(responseBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
        }
    }
}