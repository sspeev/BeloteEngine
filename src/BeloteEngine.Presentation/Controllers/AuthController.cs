using BeloteEngine.Application.User.Commands.Create;
using BeloteEngine.Application.User.Commands.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace BeloteEngine.Presentation.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController(
    ISender sender
    ) : ControllerBase
{

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateUserCommand(
                request.Username,
                request.Email,
                request.Password),
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new { Token = result.Token });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(result.Errors);
        }

        return Ok(new { Token = result.Token });
    }
}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Username, string Email, string Password);