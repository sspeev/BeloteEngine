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
        IHubContext<BeloteHub> hubContext,
        ILobbyService lobbyService,
        IGameService gameService
    ) : ControllerBase
    {
        private readonly IGameService _gameService = gameService;

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
                .SendAsync("PlayerJoined", lobby);

            return Ok(new LobbyResponse
            {
                Lobby = lobby,
                ResInfo = "OK"
            });
        }

        [HttpGet("listLobbies")]
        public IActionResult GetAvailableLobbies()
        {
            var lobbies = lobbyService.GetAvailableLobbies();
            return Ok(new LobbyResponse
            {
                Lobbies = lobbies.ToArray(),
                ResInfo = "OK"
            });
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinLobby([FromBody] RequestInfoModel request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");
            if(request.LobbyId == 0)
                return BadRequest("Lobby id cannot be empty.");

            var player = new Player { Name = request.PlayerName, LobbyId = request.LobbyId };
            var joinResult = lobbyService.JoinLobby(player);
            if (!joinResult.Success)
                return BadRequest(joinResult.ErrorMessage);

            var lobby = lobbyService.GetLobby(request.LobbyId);

            await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                .SendAsync("PlayerJoined", lobby);

            return Ok(new LobbyResponse
            {
                Lobby = lobby,
                ResInfo = joinResult.ErrorMessage ?? "OK"
            });
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveLobby([FromBody] LeaveRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
                return BadRequest("Player name cannot be empty.");

            var player = new Player { Name = request.PlayerName, LobbyId = request. LobbyId };
    
            // Check if the leaving player is the host BEFORE removing them
            var lobbyBeforeLeave = lobbyService.GetLobby(request.LobbyId);
            // if (lobbyBeforeLeave == null)
            //     return NotFound("Lobby not found.");
        
            var isLeavingPlayerHost = lobbyBeforeLeave. ConnectedPlayers.Any(p => 
                p != null && 
                p.Name == request.PlayerName && 
                p.Hoster);
    
            var success = lobbyService.LeaveLobby(player, request. LobbyId);

            if (success)
            {
                var lobby = lobbyService.GetLobby(request. LobbyId);
        
                // If the host left and lobby is now empty or should be closed
                if (isLeavingPlayerHost && (lobby == null || lobby.ConnectedPlayers.Count == 0))
                {
                    // Notify all clients that the lobby is closing
                    await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                        .SendAsync("LobbyDeleted", new
                        {
                            LobbyId = request.LobbyId,
                            Reason = "Host left the lobby"
                        });
            
                    if (lobby != null)
                    {
                        lobbyService.ResetLobby(lobby.Id);
                    }
            
                    return Ok(new { Success = success, LobbyDeleted = true });
                }
        
                // Normal case: player left but lobby continues
                if (lobby != null)
                {
                    // Send the complete lobby object with updated ConnectedPlayers
                    await hubContext.Clients.Group($"Lobby_{request.LobbyId}")
                        .SendAsync("PlayerLeft", lobby);
                }
            }

            return Ok(new { Success = success, LobbyDeleted = false });
        }

        [HttpGet("{lobbyId}")]
        public IActionResult GetLobbyState(int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found.");

            return Ok(new
            {
                Lobby = new {
                    Id = lobby.Id,
                    Name = lobby.Name,
                    ConnectedPlayers = lobby.ConnectedPlayers.ToArray(),
                    GamePhase = lobby.gamePhase,
                    GameStarted = lobby.GameStarted,
                    PlayerCount = lobby.ConnectedPlayers.Count
                }
            });
        }
    }
}