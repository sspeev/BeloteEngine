using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.Extensions.Logging;

namespace BeloteEngine.Services.Services
{
    public class LobbyService(
        ILobby _lobby
        ,IGameService _gameService 
        ,ILogger<LobbyService> _logger)
        : ILobbyService
    {
        private readonly ILobby lobby = _lobby;
        private readonly IGameService gameService = _gameService;
        private readonly ILogger<LobbyService> logger = _logger;

        //public ILobby CreateLobby()
        //{
        //    lock (lobby.LobbyLock)
        //    {
        //        lobby.Reset();
        //        lobby.Game = gameService.Creator();
        //        logger.LogInformation("New lobby created at {CreatedAt}", DateTime.UtcNow);
        //        return lobby;
        //    }
        //}

        public Task<JoinResult> JoinLobby(Player player)
        {
            lock (lobby.LobbyLock)
            {
                if (lobby.IsFull())
                {
                    return Task.FromResult(new JoinResult
                    {
                        Success = false,
                        ErrorMessage = "Lobby is full."
                    });
                }
                if (lobby.ConnectedPlayers.Any(p => p.ConnectionId == player.ConnectionId))
                {
                    return Task.FromResult(new JoinResult
                    {
                        Success = false,
                        ErrorMessage = "Player already connected."
                    });
                }
                lobby.ConnectedPlayers.Add(player);
                if (lobby.ConnectedPlayers.Count == 4)
                {
                    lobby.GameStarted = true;
                    gameService.GameInitializer();
                }
                return Task.FromResult(new JoinResult { Success = true });
            }
        }

        public Task<bool> LeaveLobby(Player player)
        {
            lock (lobby.LobbyLock)
            {
                if (!lobby.ConnectedPlayers.Remove(player))
                {
                    return Task.FromResult(false);
                }
                if (lobby.ConnectedPlayers.Count < 4)
                {
                    lobby.GameStarted = false;
                }
                return Task.FromResult(true);
            }
        }

        public Task NotifyLobbyUpdate()
        {
            logger.LogInformation("Lobby updated. Current player count: {PlayerCount}, Game started: {GameStarted}",
                lobby.ConnectedPlayers.Count,
                lobby.GameStarted);

            // Add your notification logic here
            // For example, return a Task that will be used by SignalR to notify clients

            return Task.CompletedTask;
        }

        public void ResetLobby()
        {
            lock (lobby.LobbyLock)
            {
                lobby.Reset();
            }
        }
    }
}
