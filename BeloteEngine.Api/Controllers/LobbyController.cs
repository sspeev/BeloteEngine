using BeloteEngine.Api.Hubs;
using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LobbyController(
        IHubContext<BeloteHub> _hubContext,
        ILobbyService _lobbyService,
        IGameService _gameService
    ) : ControllerBase
    {
        private readonly IHubContext<BeloteHub> hubContext = _hubContext;
        private readonly ILobbyService lobbyService = _lobbyService;
        private readonly IGameService gameService = _gameService;

        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby([FromBody] RequestInfoModel request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");
            if (string.IsNullOrWhiteSpace(request.LobbyName))
                return BadRequest("Lobby name cannot be empty.");

            var lobby = lobbyService.CreateLobby(request.LobbyName);

            var player = new Player()
            {
                Name = request.PlayerName,
                LobbyId = lobby.Id,
                Hoster = true
            };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
                return BadRequest(joinResult.ErrorMessage);

            await hubContext.Clients.Group($"Lobby_{lobby.Id}")
                .SendAsync("PlayersUpdated", lobby.ConnectedPlayers);

            return Ok(new
            {
                lobby = new
                {
                    lobby.Id,
                    lobby.Name,
                    connectedPlayers = lobby.ConnectedPlayers
                }
            });
        }

        [HttpGet("listLobbies")]
        public IActionResult GetAvailableLobbies()
        {
            var lobbies = lobbyService.GetAvailableLobbies();
            return Ok(lobbies);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinLobby([FromBody] RequestInfoModel request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");

            var player = new Player { Name = request.PlayerName, LobbyId = request.LobbyId };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
                return BadRequest(joinResult.ErrorMessage);

            var lobby = lobbyService.GetLobby(request.LobbyId);

            await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                .SendAsync("PlayersUpdated", lobby.ConnectedPlayers);

            if (lobby.ConnectedPlayers.Count == 4)
            {
                gameService.GameInitializer(lobby);
                await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                    .SendAsync("GameStarted", new { request.LobbyId });
            }

            return Ok(new
            {
                joinResult.Lobby,
                joinResult.ErrorMessage
            });
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveLobby([FromBody] LeaveRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");

            var player = new Player { Name = request.PlayerName, LobbyId = request.LobbyId };
            var success = lobbyService.LeaveLobby(player, request.LobbyId);

            if (success)
            {
                var lobby = lobbyService.GetLobby(request.LobbyId);
                await hubContext.Clients.All.SendAsync("PlayerLeft", new
                {
                    LobbyId = request.LobbyId,
                    PlayerName = request.PlayerName,
                    PlayerCount = lobby?.ConnectedPlayers.Count ?? 0
                });
                var isHosterHere = lobby.ConnectedPlayers.Any(p => p.Hoster);
                if (!isHosterHere)
                {
                    lobbyService.ResetLobby(lobby.Id);
                    return Ok(new { Success = success, IsHosterHere = isHosterHere });
                }
            }

            return Ok(new { Success = success });
        }

        [HttpGet("{lobbyId}")]
        public IActionResult GetLobbyState(int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found.");

            return Ok(new
            {
                Lobby = lobby
            });
        }
    }
}