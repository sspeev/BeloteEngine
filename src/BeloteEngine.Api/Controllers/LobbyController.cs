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
    ILobbyService lobbyService
) : ControllerBase
{
    /// <summary>
    /// Creates a new lobby with the specified settings and adds the requesting client to the lobby.
    /// </summary>
    /// <remarks>This action requires a valid request body. If the request is invalid or an error
    /// occurs while adding the client to the lobby, a bad request response is returned. The response includes a
    /// location header pointing to the endpoint for retrieving the lobby state.</remarks>
    /// <param name="request">The information required to create the lobby, including the lobby name and any additional configuration.
    /// Must not be null and must satisfy all model validation requirements.</param>
    /// <returns>A response containing the details of the newly created lobby if successful; otherwise, a bad request
    /// response with validation or error details.</returns>
    [HttpPost("create")]
    public async Task<IActionResult> CreateLobby([FromBody] RequestInfoModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var lobby = lobbyService.CreateLobby(request.LobbyName);

        return CreatedAtAction(
            nameof(GetLobby),
            new { lobbyId = lobby.Id },
            new LobbyResponse
            {
                Lobby = lobby
            });
    }

    /// <summary>
    /// Retrieves a list of all available lobbies.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="LobbyResponse"/> with the collection of available
    /// lobbies. The response will contain an empty collection if no lobbies are available.</returns>
    [HttpGet("listLobbies")]
    public IActionResult GetAvailableLobbies()
    {
        var lobbies = lobbyService.GetAvailableLobbies();
        return Ok(new LobbyResponse
        {
            Lobbies = [.. lobbies]
        });
    }

    /// <summary>
    /// Retrieves the current state of the specified lobby, including player and game information.
    /// </summary>
    /// <remarks>The returned lobby state includes the lobby's ID, name, list of connected players,
    /// current game phase, whether the game has started, and the total number of connected players.</remarks>
    /// <param name="lobbyId">The unique identifier of the lobby to retrieve. Must correspond to an existing lobby.</param>
    /// <returns>An <see cref="IActionResult"/> containing the lobby state if found; otherwise, a NotFound result if the
    /// lobby does not exist.</returns>
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