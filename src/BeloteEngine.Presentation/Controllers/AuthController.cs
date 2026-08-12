using BeloteEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BeloteEngine.Application.Contracts;

namespace BeloteEngine.Presentation.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider
    ) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            return Ok();
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
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
