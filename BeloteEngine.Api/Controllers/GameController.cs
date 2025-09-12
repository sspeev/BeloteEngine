using BeloteEngine.Api.Hubs;
using Microsoft.AspNetCore.Mvc;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/{lobbyId}")]
    [ApiController]
    public class GameController(
        ILogger<GameController> _logger
        ,BeloteHub _hub
        ) : ControllerBase
    {
        private readonly ILogger<GameController> logger = _logger;
        private readonly BeloteHub hub = _hub;

        [HttpGet($"start")]
        public async Task<IActionResult> StartGame(int lobbyId)
        {
            await hub.OnConnectedAsync();
            return Ok("Game logic will be implemented here.");
        }
    }
}
