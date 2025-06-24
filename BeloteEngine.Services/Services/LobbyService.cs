using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;

namespace BeloteEngine.Services.Services
{
    public class LobbyService(ILobby _lobby, IGameService _gameService) : ILobbyService
    {
        private readonly ILobby lobby = _lobby;
        private readonly IGameService gameService = _gameService;

        public Task HandlePlayerDisconnection(Player player)
        {
            throw new NotImplementedException();
        }

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
