using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BeloteEngine.Services.Services
{
    public class LobbyService(
        IGameService _gameService
        , ILogger<LobbyService> _logger)
        : ILobbyService
    {
        private readonly IGameService gameService = _gameService;
        private readonly ILogger<LobbyService> logger = _logger;
        private readonly ConcurrentDictionary<int, Lobby> lobbies = new();
        private readonly object lockObject = new();

        public Lobby CreateLobby()
        {
            var lobby = new Lobby
            {
                Game = gameService.Creator()
            };
            
            // Generate unique ID and add to dictionary
            int lobbyId;
            do {
                lobbyId = new Random().Next(1000, 9999);
            } while (lobbies.ContainsKey(lobbyId));
            
            lobby.Id = lobbyId;
            lobbies.TryAdd(lobbyId, lobby);
            
            logger.LogInformation("New lobby created with ID {LobbyId}", lobbyId);
            return lobby;
        }

        public JoinResult JoinLobby(Player player)
        {
            int lobbyId = player.ConnectionId ?? 0;
            if (lobbyId == 0)
            {
                return new JoinResult
                {
                    Success = false,
                    ErrorMessage = "Invalid lobby ID."
                };
            }
            if (!lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return new JoinResult
                {
                    Success = false,
                    ErrorMessage = $"Lobby {lobbyId} does not exist."
                };
            }

            lock (lockObject)
            {
                if (IsFull(lobbyId))
                {
                    return new JoinResult
                    {
                        Success = false,
                        ErrorMessage = "Lobby is full."
                    };
                }
                
                if (lobby.ConnectedPlayers.Any(p => p.ConnectionId == player.ConnectionId))
                {
                    return new JoinResult
                    {
                        Success = false,
                        ErrorMessage = "Player already connected."
                    };
                }
                
                lobby.ConnectedPlayers.Add(player);
                
                return new JoinResult { Success = true };
            }
        }

        public bool LeaveLobby(Player player, int lobbyId)
        {
            if (!lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            lock (lockObject)
            {
                var playerToRemove = lobby.ConnectedPlayers
                    .FirstOrDefault(p => p.ConnectionId == player.ConnectionId);
                    
                if (playerToRemove == null)
                {
                    return false;
                }
                
                lobby.ConnectedPlayers.Remove(playerToRemove);
                
                if (lobby.ConnectedPlayers.Count < 4)
                {
                    lobby.GameStarted = false;
                }
                
                // Remove lobby if empty
                if (lobby.ConnectedPlayers.Count == 0)
                {
                    lobbies.TryRemove(lobbyId, out _);
                    logger.LogInformation("Lobby {LobbyId} removed as it's empty", lobbyId);
                }
                
                return true;
            }
        }

        public Task NotifyLobbyUpdate(int lobbyId)
        {
            if (lobbies.TryGetValue(lobbyId, out var lobby))
            {
                logger.LogInformation("Lobby {LobbyId} updated. Players: {PlayerCount}, Game started: {GameStarted}",
                    lobbyId, lobby.ConnectedPlayers.Count, lobby.GameStarted);
            }
            return Task.CompletedTask;
        }

        public void ResetLobby(int lobbyId)
        {
            if (lobbies.TryGetValue(lobbyId, out var lobby))
            {
                lock (lockObject)
                {
                    lobby.ConnectedPlayers.Clear();
                    lobby.GameStarted = false;
                    // Assuming Reset() resets the game state
                    lobby.Game = gameService.Creator();
                }
            }
        }
        public bool IsFull(int lobbyId) => lobbies[lobbyId].ConnectedPlayers.Count >= 4;

        public Lobby GetLobby(int lobbyId)
        {
            if (lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return lobby;
            }
            else
            {
                logger.LogWarning("Lobby {LobbyId} not found", lobbyId);
                return null!;
            }
        }
    }
}
