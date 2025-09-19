using BeloteEngine.Api.Hubs;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/{lobbyId}")]
    [ApiController]
    public class GameController(
        ILogger<GameController> _logger
        , IGameService _gameService
        , ILobbyService _lobbyService
        //,BeloteHub _hub
        ) : ControllerBase
    {
        private readonly ILogger<GameController> logger = _logger;
        private readonly IGameService gameService = _gameService;
        private readonly ILobbyService lobbyService = _lobbyService;
        //private readonly BeloteHub hub = _hub;

        [HttpGet($"start")]
        public IActionResult StartGame([FromRoute] int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            var game = gameService.InitialPhase(lobby);
            return Ok(new
            {
                Game = game
            });
        }
    }
}
