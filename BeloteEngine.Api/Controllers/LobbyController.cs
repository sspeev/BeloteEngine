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
        , ILobbyService _lobbyService
        , IGameService _gameService
        , ILobby _lobby) : ControllerBase
    {
        //private readonly ILogger<GameController> logger = _logger;
        private readonly IHubContext<BeloteHub> hubContext = _hubContext;
        private readonly ILobbyService lobbyService = _lobbyService;
        private readonly IGameService gameService = _gameService;
        private readonly ILobby lobby = _lobby;

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] PlayerNameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Player name cannot be empty.");
            }

            var player = new Player { Name = request.Name, ConnectionId = 1, IsConnected = true };
            var joinResult = await lobbyService.JoinLobby(player);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Welcome to the Belote Lobby!");
            return Ok();
        }

        //[HttpPost("create")]
        //public IActionResult CreateLobby([FromBody] PlayerNameRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.Name))
        //    {
        //        return BadRequest("Player name cannot be empty.");
        //    }
            
        //    // 1. First reset/prepare the lobby
        //    lobby.Reset();
            
        //    // 2. Create and add the player
        //    var player = new Player { Name = request.Name, ConnectionId = 1, IsConnected = true };
        //    var joinResult = lobbyService.JoinLobby(player).Result;
        //    if (!joinResult.Success)
        //    {
        //        return BadRequest(joinResult.ErrorMessage);
        //    }
            
        //    // 3. Return lobby info (without initializing game yet)
        //    return Ok(new { LobbyId = lobby.CreatedAt.Ticks, PlayerCount = lobby.ConnectedPlayers.Count });
            
        //    // Game initialization should happen later, when all players have joined
        //}

        public class PlayerNameRequest
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
