using BeloteEngine.Api.Contracts;
using BeloteEngine.Api.Models;
using BeloteEngine.Api.Services;
using BeloteEngine.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace BeloteEngine.Api.Controllers
{
    [Route("api/session")]
    [ApiController]
    public sealed class SessionController(
        ISessionService _sessionCookieService) : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateSession()
        {
            _sessionCookieService.IssueSessionCookie(Request, Response);
            return NoContent();
        }

        [HttpPatch]
        public IActionResult SetSession([FromBody] SessionRequestModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string sanitizedPlayerName;
            try
            {
                sanitizedPlayerName = InputValidator.SanitizePlayerName(request.PlayerName);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(request.PlayerName), ex.Message);
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
}
