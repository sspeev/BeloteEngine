using BeloteEngine.Services.Contracts;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BeloteEngine.Services.Services
{
    public class WebSocketService : IWebSocketService
    {
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketService(ILogger<WebSocketService> logger)
        {
            _logger = logger;
        }

        public async Task HandleConnectionAsync(WebSocket webSocket, string playerId, string lobbyId)
        {
            _logger.LogInformation("WebSocket connection established for player {PlayerId} in lobby {LobbyId}", playerId, lobbyId);
            
            var buffer = new byte[1024 * 4];

            try
            {
                // Send welcome message immediately after connection
                var welcomeMessage = JsonSerializer.Serialize(new
                {
                    type = "connection_established",
                    payload = new { playerId, lobbyId, message = "Welcome to the lobby!" }
                });
                
                var welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(welcomeBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInformation("Received message from {PlayerId}: {Message}", playerId, message);

                        // Try to parse as JSON
                        try
                        {
                            var messageObj = JsonDocument.Parse(message);
                            var messageType = messageObj.RootElement.GetProperty("type").GetString();
                            
                            var response = await HandleMessage(messageType, messageObj, playerId, lobbyId);
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(responseBytes),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
                        }
                        catch (JsonException)
                        {
                            // If not JSON, treat as plain text and echo back
                            var responseMessage = $"Echo from lobby {lobbyId}: {message}";
                            var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(responseBytes),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket close requested by player {PlayerId}", playerId);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by client", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error for player {PlayerId}: {Error}", playerId, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error for player {PlayerId}: {Error}", playerId, ex.Message);
            }
            finally
            {
                _logger.LogInformation("WebSocket connection closed for player {PlayerId}", playerId);
            }
        }

        private async Task<string> HandleMessage(string messageType, JsonDocument messageObj, string playerId, string lobbyId)
        {
            return messageType switch
            {
                "connection_established" => JsonSerializer.Serialize(new
                {
                    type = "connection_confirmed",
                    payload = new { playerId, lobbyId, status = "connected" }
                }),
                "join_lobby" => await HandleJoinLobby(messageObj, playerId, lobbyId),
                "leave_lobby" => await HandleLeaveLobby(messageObj, playerId, lobbyId),
                "play_card" => await HandlePlayCard(messageObj, playerId, lobbyId),
                "make_bid" => await HandleMakeBid(messageObj, playerId, lobbyId),
                "get_game_state" => await HandleGetGameState(messageObj, playerId, lobbyId),
                "ping" => JsonSerializer.Serialize(new { type = "pong", payload = new { timestamp = DateTime.UtcNow } }),
                _ => JsonSerializer.Serialize(new
                {
                    type = "error",
                    payload = new { message = $"Unknown message type: {messageType}" }
                })
            };
        }

        private async Task<string> HandleJoinLobby(JsonDocument messageObj, string playerId, string lobbyId)
        {
            // Implement lobby joining logic here
            return JsonSerializer.Serialize(new
            {
                type = "lobby_joined",
                payload = new { playerId, lobbyId, success = true }
            });
        }

        private async Task<string> HandleLeaveLobby(JsonDocument messageObj, string playerId, string lobbyId)
        {
            // Implement lobby leaving logic here
            return JsonSerializer.Serialize(new
            {
                type = "lobby_left",
                payload = new { playerId, lobbyId, success = true }
            });
        }

        private async Task<string> HandlePlayCard(JsonDocument messageObj, string playerId, string lobbyId)
        {
            // Implement card playing logic here
            return JsonSerializer.Serialize(new
            {
                type = "card_played",
                payload = new { playerId, lobbyId, success = true }
            });
        }

        private async Task<string> HandleMakeBid(JsonDocument messageObj, string playerId, string lobbyId)
        {
            // Implement bidding logic here
            return JsonSerializer.Serialize(new
            {
                type = "bid_made",
                payload = new { playerId, lobbyId, success = true }
            });
        }

        private async Task<string> HandleGetGameState(JsonDocument messageObj, string playerId, string lobbyId)
        {
            // Implement game state retrieval logic here
            return JsonSerializer.Serialize(new
            {
                type = "game_state",
                payload = new { playerId, lobbyId, gameState = "active" }
            });
        }
    }
}