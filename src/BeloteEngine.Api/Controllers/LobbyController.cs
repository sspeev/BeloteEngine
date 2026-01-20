using BeloteEngine.Api.Hubs;
using BeloteEngine.Api.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using System.Net.Mime;

namespace BeloteEngine.Api.Controllers
{
    [EnableRateLimiting("fixed")]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public sealed class LobbyController(
        IHubContext<BeloteHub, IBeloteClient> hubContext,
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

            return CreatedAtAction(
                nameof(GetLobbyState),
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
        /// Adds the requesting user to the specified lobby and returns the updated lobby information.
        /// </summary>
        /// <param name="request">The request data containing the lobby identifier and user details. Must not be null and must satisfy all
        /// model validation requirements.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated lobby information if the operation succeeds;
        /// otherwise, a bad request result with error details.</returns>
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

        /// <summary>
        /// Removes the current user from the specified lobby.
        /// </summary>
        /// <remarks>Returns a bad request response if the request model is invalid. The lobby may be
        /// deleted if the user leaving is the last participant.</remarks>
        /// <param name="request">The request containing the lobby identifier and user information required to leave the lobby. Cannot be
        /// null.</param>
        /// <returns>An IActionResult containing the result of the leave operation. The response includes a success flag and
        /// indicates whether the lobby was deleted as a result.</returns>
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

        /// <summary>
        /// Retrieves the current state of the specified lobby, including player and game information.
        /// </summary>
        /// <remarks>The returned lobby state includes the lobby's ID, name, list of connected players,
        /// current game phase, whether the game has started, and the total number of connected players.</remarks>
        /// <param name="lobbyId">The unique identifier of the lobby to retrieve. Must correspond to an existing lobby.</param>
        /// <returns>An <see cref="IActionResult"/> containing the lobby state if found; otherwise, a NotFound result if the
        /// lobby does not exist.</returns>
        [HttpGet("{lobbyId:int}")]
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