using BeloteEngine.Api.Contracts;
using BeloteEngine.Services.Security;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace BeloteEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public sealed class SessionController(
    ISessionService _sessionCookieService) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateSession()
    {
        _sessionCookieService.IssueSessionCookie(Request, Response);
        return NoContent();
    }

    [HttpPatch("{playerName}")]
    public IActionResult SetSession(string playerName)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        string sanitizedPlayerName;
        try
        {
            sanitizedPlayerName = InputValidator.SanitizePlayerName(playerName);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(playerName), ex.Message);
            return ValidationProblem(ModelState);
        }

        _sessionCookieService.SetSessionCookie(Request, Response, sanitizedPlayerName);
        return Ok(new { playerName = sanitizedPlayerName });
    }

    [HttpDelete]
    public IActionResult DeleteSession()
    {
        _sessionCookieService.ClearSessionCookie(Request, Response);
        return NoContent();
    }
}
