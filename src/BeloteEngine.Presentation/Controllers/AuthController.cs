using BeloteEngine.Application.User.Commands.Register;
using BeloteEngine.Application.User.Commands.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using BeloteEngine.Presentation.Common;
using BeloteEngine.Presentation.Models;

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
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
    [FromBody] RegisterRequest request,
    CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterUserCommand(request.Username, request.Email, request.Password),
            cancellationToken);

        if (result.Succeeded)
        {
            var authResponse = new AuthResponse
            {
                Token = result.Token!
            };
            return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Registration successful"));
        }

        return BadRequest(ApiResponse<AuthResponse>.Fail(result.Errors, "Registration failed"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Errors, "Invalid credentials"));
        }

        var authResponse = new AuthResponse { Token = result.Token! };
        return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Login successful"));
    }

}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Username, string Email, string Password);