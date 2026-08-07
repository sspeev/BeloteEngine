using BeloteEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeloteEngine.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager
    ) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

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
    public IActionResult Login(string userName, string password)
    {
        return Ok();
    }
}
