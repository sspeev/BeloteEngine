using BeloteEngine.Api.Hubs;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LobbyController(
        //ILogger<GameController> _logger
        IHubContext<BeloteHub> _hubContext
        , ILobbyService _lobbyService) : ControllerBase
    {
        //private readonly ILogger<GameController> logger = _logger;
        private readonly IHubContext<BeloteHub> hubContext = _hubContext;
        private readonly ILobbyService lobbyService = _lobbyService;

        [HttpGet]
        public async Task<IActionResult> Join()
        {
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Welcome to the Belote Lobby!");
            return Ok(new List<int>() { 1, 2, 3 });
        }

        [HttpPost("create")]
        public IActionResult CreateLobby([FromBody] PlayerNameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Player name cannot be empty.");
            }
            
            var player = new Player { Name = request.Name, ConnectionId = 1, IsConnected = true };
            var lobby = lobbyService.CreateLobby();
            
            var joinResult = lobbyService.JoinLobby(player).Result;
            if (!joinResult.Success)
            {
                return BadRequest(joinResult.ErrorMessage);
            }
            
            return Ok(new { LobbyId = lobby.CreatedAt.Ticks, PlayerCount = lobby.ConnectedPlayers.Count });
        }

        public class PlayerNameRequest
        {
            public string Name { get; set; }
        }
    }
}
