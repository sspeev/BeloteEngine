using BeloteEngine.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LobbyController(
        //ILogger<GameController> _logger
        BeloteHub _hub
        ,IHubContext<BeloteHub> _hubContext) : ControllerBase
    {
        //private readonly ILogger<GameController> logger = _logger;
        private readonly BeloteHub hub = _hub;
        private readonly IHubContext<BeloteHub> hubContext = _hubContext;

        [HttpGet]
        public async Task<IActionResult> Join()
        {
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Welcome to the Belote Lobby!");
            return Ok("Game logic will be implemented here.");
        }
    }
}
