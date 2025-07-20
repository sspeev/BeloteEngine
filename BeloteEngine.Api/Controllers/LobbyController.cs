using BeloteEngine.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LobbyController(
        //ILogger<GameController> _logger
        IHubContext<BeloteHub> _hubContext) : ControllerBase
    {
        //private readonly ILogger<GameController> logger = _logger;
        private readonly IHubContext<BeloteHub> hubContext = _hubContext;

        [HttpGet]
        public async Task<IActionResult> Join()
        {
            //await hubContext.Clients.All.SendAsync("ReceiveMessage", "Welcome to the Belote Lobby!");
            return Ok(new List<int>() { 1, 2, 3});
        }
    }
}
