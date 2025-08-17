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
        public async Task<IActionResult> CreateLobby([FromBody] RequestInfo request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");
            if (string.IsNullOrWhiteSpace(request.LobbyName))
                return BadRequest("Lobby name cannot be empty.");

            var lobby = lobbyService.CreateLobby(request.LobbyName);

            // Note: ConnectionId is (mis)used here to carry LobbyId for JoinLobby
            var player = new Player { Name = request.PlayerName, ConnectionId = lobby.Id, IsConnected = true };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
                return BadRequest(joinResult.ErrorMessage);

            // Notify only the lobby group with the full players list
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
        public async Task<IActionResult> Join([FromBody] RequestInfo request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");

            // Note: ConnectionId carries LobbyId for LobbyService.JoinLobby
            var player = new Player { Name = request.PlayerName, ConnectionId = request.LobbyId, IsConnected = true };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
                return BadRequest(joinResult.ErrorMessage);

            var lobby = lobbyService.GetLobby(request.LobbyId);

            // Broadcast to the lobby group only, and send the full player list so all tabs can render the same count
            await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                .SendAsync("PlayersUpdated", lobby.ConnectedPlayers);

            if (lobby.ConnectedPlayers.Count == 4)
            {
                gameService.GameInitializer(lobby);
                await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                    .SendAsync("GameStarted", new { LobbyId = request.LobbyId });
            }

            return Ok(new
            {
                joinResult.Success,
                joinResult.ErrorMessage,
                LobbyId = request.LobbyId,
                PlayerName = request.PlayerName,
                PlayerCount = lobby.ConnectedPlayers.Count
            });
        }

        //    [HttpPost("leave")]
        //    public async Task<IActionResult> Leave([FromBody] LeaveRequest request)
        //    {
        //        if (string.IsNullOrWhiteSpace(request.PlayerName))
        //            return BadRequest("Player name cannot be empty.");

        //        var player = new Player { Name = request.PlayerName, ConnectionId = request.LobbyId };
        //        var success = lobbyService.LeaveLobby(player, request.LobbyId);

        //        if (success)
        //        {
        //            var lobby = lobbyService.GetLobby(request.LobbyId);
        //            await hubContext.Clients.All.SendAsync("PlayerLeft", new {
        //                LobbyId = request.LobbyId,
        //                PlayerName = request.PlayerName,
        //                PlayerCount = lobby?.ConnectedPlayers.Count ?? 0
        //            });
        //        }

        //        return Ok(new { Success = success });
        //    }

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
        //}

        //public class LeaveRequest
        //{
        //    public string PlayerName { get; set; } = string.Empty;
        //    public int LobbyId { get; set; }
        //}
    }
}