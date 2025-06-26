using BeloteEngine.Api.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/lobby")]
    [ApiController]
    public class GameController(
        //ILogger<GameController> _logger
        //,BeloteHub _hub
        ) : ControllerBase
    {
        //private readonly ILogger<GameController> logger = _logger;
        //private readonly BeloteHub hub = _hub;

        //[HttpGet]
        //public async Task<IActionResult> Play()
        //{
        //    await hub.OnConnectedAsync();
        //    return Ok("Game logic will be implemented here.");
        //}
    }
}
