using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Services;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Hubs
{
    public class BeloteHub(
        ILogger<BeloteHub> _logger
        //, IGameService _gameService
        //, ILobbyService _lobbyService
        ) : Hub
    {
        private readonly ILogger<BeloteHub> logger = _logger;
        //private readonly IGameService gameService = _gameService;
        //private readonly ILobbyService lobbyService = _lobbyService;

        public override Task OnConnectedAsync()
        {
            logger.LogInformation("Player connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        //public void StartGame()
        //{
        //    lobby.Game =  gameService.GameInitializer();
        //}

        public Task JoinGame(Player player)
        {

        }

        //public Task SendMove(string gameId, object moveData)
        //{
        //    logger.LogInformation("Move sent for game {GameId}.", gameId);
        //    return gameService.ProcessMove(gameId, moveData);
        //}
    }
}
