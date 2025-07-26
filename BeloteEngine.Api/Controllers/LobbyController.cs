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
        ) : ControllerBase
    {
        //private readonly ILogger<GameController> logger = _logger;
        private readonly IHubContext<BeloteHub> hubContext = _hubContext;
        private readonly ILobbyService lobbyService = _lobbyService;
        private readonly IGameService gameService = _gameService;

        [HttpPost("create")]
        public IActionResult CreateLobby([FromBody] RequestInfo request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Player name cannot be empty.");
            }
            var lobby = lobbyService.CreateLobby();
            var player = new Player { Name = request.Name, ConnectionId = lobby.Id, IsConnected = true };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
            {
                return BadRequest(joinResult.ErrorMessage);
            }

            return Ok(new
            {
                LobbyId = lobby.Id,
                PlayerCount = lobby.ConnectedPlayers.Count
            });
        }

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] RequestInfo request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Player name cannot be empty.");
            }

            var player = new Player { Name = request.Name, ConnectionId = request.LobbyId, IsConnected = true };
            var joinResult = lobbyService.JoinLobby(player);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", "Welcome to the Belote Lobby!");

            if(lobbyService.GetLobby(request.LobbyId).ConnectedPlayers.Count == 4)
            {
                gameService.GameInitializer(lobbyService.GetLobby(request.LobbyId));
            }
            return Ok(new
            {
                joinResult.Success,
                joinResult.ErrorMessage,
                request.LobbyId,
                PlayerCount = lobbyService.GetLobby(request.LobbyId)?.ConnectedPlayers.Count ?? 0
            });
        }

        public class RequestInfo
        {
            public string Name { get; set; } = string.Empty;

            public int LobbyId { get; set; }
        }
    }
}
