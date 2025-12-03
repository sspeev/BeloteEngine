using BeloteEngine.Api.Hubs;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/{lobbyId:int}")]
    [ApiController]
    public class GameController(
        ILogger<GameController> _logger
        , IGameService _gameService
        , ILobbyService _lobbyService
        , IHubContext<BeloteHub> _hub
        ) : ControllerBase
    {
        private readonly ILogger<GameController> logger = _logger;
        private readonly IGameService gameService = _gameService;
        private readonly ILobbyService lobbyService = _lobbyService;
        private readonly IHubContext<BeloteHub> hub = _hub;

        [HttpPost($"start")]
        public async Task<IActionResult> StartGame([FromRoute] int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            gameService.GameInitializer(lobby);
            lobby.gamePhase = "playing";
            gameService.InitialPhase(lobby);

            await hub.Clients.Group($"Lobby_{lobbyId}").SendAsync("StartGame", lobby);
            return Ok(new
            {
                Lobby = lobby
            });
        }
    }
}
