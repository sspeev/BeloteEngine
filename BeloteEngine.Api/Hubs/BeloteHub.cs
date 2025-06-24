using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BeloteEngine.Api.Hubs
{
    public class BeloteHub : Hub
    {
        private readonly ILogger<BeloteHub> logger;
        private readonly IGameService gameService;

        public BeloteHub(ILogger<BeloteHub> logger, IGameService gameService)
        {
            this.logger = logger;
            this.gameService = gameService;
        }

        public override Task OnConnectedAsync()
        {
            logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public void StartGame()
        {
            logger.LogInformation("Game started.");
            gameService.GameInitializer();
        }

        public Task JoinGame(string gameId, string playerId)
        {
            logger.LogInformation("Player {PlayerId} joined game {GameId}.", playerId, gameId);
            return gameService.JoinGame(gameId, playerId);
        }

        public Task SendMove(string gameId, object moveData)
        {
            logger.LogInformation("Move sent for game {GameId}.", gameId);
            return gameService.ProcessMove(gameId, moveData);
        }
    }
}
