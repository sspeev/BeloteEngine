using BeloteEngine.Api.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;

namespace BeloteEngine.Api.Controllers;

[EnableRateLimiting("fixed")]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public sealed class LobbyController(
    ILobbyService _lobbyService
) : ControllerBase
{

    [HttpPost("create")]
    public async Task<IActionResult> CreateLobby([FromBody] CreateRequestModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var lobby = _lobbyService.CreateLobby(request.LobbyName, ipAddress);

            return CreatedAtAction(
                nameof(GetLobby),
                new { lobbyId = lobby.Id },
                new LobbyResponse
                {
                    Lobby = lobby
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("listLobbies")]
    public IActionResult GetAvailableLobbies()
    {
        var lobbies = _lobbyService.GetAvailableLobbies();
        return Ok(new LobbyResponse
        {
            Lobbies = [.. lobbies]
        });
    }

    [HttpGet("{lobbyId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetLobby(int lobbyId)
    {
        var lobby = _lobbyService.GetLobby(lobbyId);
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
                Game = lobby.Game,
                PlayerCount = lobby.ConnectedPlayers.Count
            }
        });
    }
}
