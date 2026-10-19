using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;

namespace BeloteEngine.Presentation.Controllers;

[EnableRateLimiting("fixed")]
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public sealed class LobbyController(
    ILobbyService lobbyService
    , IUserIdentityService userIdentityService
) : ControllerBase
{

    [HttpPost("create")]
    public async Task<IActionResult> CreateLobby(
        [FromBody] CreateRequestModel request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await userIdentityService.FindByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
                return NotFound("User not found.");

            var creator = new Player
            {
                UserId = user.Id,
                Name = request.PlayerName,
                Hoster = true
            };

            var lobby = lobbyService.CreateLobby(request.LobbyName, creator);

            return CreatedAtAction(
                nameof(GetLobby),
                new { lobbyId = lobby.Id },
                new LobbyResponse
                {
                    Lobby = LobbyResponseModel.FromDomain(lobby)
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
        var lobbies = lobbyService.GetAvailableLobbies();
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
        var lobby = lobbyService.GetLobby(lobbyId);
        if (lobby == null)
            return NotFound("Lobby not found.");

        return Ok(new
        {
            Lobby = LobbyResponseModel.FromDomain(lobby)
        });
    }
}
