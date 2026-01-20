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
        IHubContext<BeloteHub, IBeloteClient> hubContext,
        ILobbyService lobbyService,
        IGameService gameService
    ) : ControllerBase
    {
        private readonly IGameService _gameService = gameService;

        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby([FromBody] RequestInfoModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var lobby = lobbyService.CreateLobby(request.LobbyName);
            try
            {
                await hubContext.Clients.Group($"Lobby_{lobby.Id}").JoinLobby(lobby.Id, request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new LobbyResponse
            {
                Lobby = lobby
            });
        }

        [HttpGet("listLobbies")]
        public IActionResult GetAvailableLobbies()
        {
            var lobbies = lobbyService.GetAvailableLobbies();
            return Ok(new LobbyResponse
            {
                Lobbies = lobbies.ToArray()
            });
        }

        [HttpPut("join")]
        public async Task<IActionResult> JoinLobby([FromBody] RequestInfoModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var lobby = lobbyService.GetLobby(request.LobbyId);
            try
            {
                await hubContext.Clients.Group($"Lobby_{request.LobbyId}").JoinLobby(request.LobbyId, request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new LobbyResponse
            {
                Lobby = lobby
            });
        }

        [HttpDelete("leave")]
        public async Task<IActionResult> LeaveLobby([FromBody] LeaveRequestModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await hubContext.Clients
                .Group($"Lobby_{request.LobbyId}")
                .LeaveLobby(request);

            return Ok(new
            {
                Success = result.IsLeaveSuccessfull,
                LobbyDeleted = result.IsDeletingSuccessfull
            });
        }

        [HttpGet("{lobbyId}")]
        public IActionResult GetLobbyState(int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found.");

            return Ok(new
            {
                Lobby = new
                {
                    Id = lobby.Id,
                    Name = lobby.Name,
                    ConnectedPlayers = lobby.ConnectedPlayers.ToArray(),
                    GamePhase = lobby.GamePhase,
                    GameStarted = lobby.GameStarted,
                    PlayerCount = lobby.ConnectedPlayers.Count
                }
            });
        }
    }
}