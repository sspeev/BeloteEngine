using BeloteEngine.Api.Hubs;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/{lobbyId:int}")]
    [ApiController]
    public class GameController(
        IGameService gameService
        , ILobbyService lobbyService
        , IHubContext<BeloteHub> hub
        ) : ControllerBase
    {
        [HttpPost("start")]
        public async Task<IActionResult> StartGame([FromRoute] int lobbyId)
        {
            var lobby = lobbyService.GetLobby(lobbyId);
            gameService.GameInitializer(lobby);
            lobby.GamePhase = "playing";
            gameService.InitialPhase(lobby);

            await hub.Clients.Group($"Lobby_{lobbyId}").SendAsync("StartGame", lobby);
            return Ok(new
            {
                Lobby = lobby
            });
        }
    }
}
