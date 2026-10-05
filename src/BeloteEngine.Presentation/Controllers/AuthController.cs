using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.User.Commands.Create;
using BeloteEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace BeloteEngine.Presentation.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider,
    ISender sender
    ) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

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
            return Ok();
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = _jwtProvider.GenerateToken(user.Id, user.UserName!);

        return Ok(new
        {
            Token = token
        });
    }
}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Username, string Email, string Password);