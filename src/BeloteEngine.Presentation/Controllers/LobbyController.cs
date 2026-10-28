using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;
using BeloteEngine.Presentation.Common;

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
    public async Task<ActionResult<ApiResponse<LobbyResponse>>> CreateLobby(
        [FromBody] CreateRequestModel request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<LobbyResponse>.Fail(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

        try
        {
            var user = await userIdentityService.FindByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
                return NotFound(ApiResponse<LobbyResponse>.Fail("User not found."));

            var creator = new Player
            {
                UserId = user.Id,
                Name = request.PlayerName,
                Hoster = true
            };

            var lobby = lobbyService.CreateLobby(request.LobbyName, creator);

            var response = new LobbyResponse
            {
                Lobby = LobbyResponseModel.FromDomain(lobby)
            };

            return CreatedAtAction(
                nameof(GetLobby),
                new { lobbyId = lobby.Id },
                ApiResponse<LobbyResponse>.Ok(response, "Lobby created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LobbyResponse>.Fail(ex.Message));
        }
    }

    [HttpGet("listLobbies")]
    public IActionResult GetAvailableLobbies()
    {
        var lobbies = lobbyService.GetAvailableLobbies();
        var response = new LobbyResponse
        {
            Lobbies = [.. lobbies]
        };
        return Ok(ApiResponse<LobbyResponse>.Ok(response));
    }

    [HttpGet("{lobbyId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetLobby(int lobbyId)
    {
        var lobby = lobbyService.GetLobby(lobbyId);
        if (lobby == null)
            return NotFound(ApiResponse<LobbyResponse>.Fail("Lobby not found."));

        var response = new LobbyResponse
        {
            Lobby = LobbyResponseModel.FromDomain(lobby)
        };

        return Ok(ApiResponse<LobbyResponse>.Ok(response));
    }
}