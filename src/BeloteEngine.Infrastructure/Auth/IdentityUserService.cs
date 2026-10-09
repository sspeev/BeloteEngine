using System.Text;
using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BeloteEngine.Infrastructure.Services;

public sealed class IdentityUserService(
    UserManager<IdentityUser> userManager
    , IJwtProvider jwtProvider) : IUserIdentityService
{
    public async Task<IdentityOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if(user is null ||
            !await userManager.CheckPasswordAsync(user, password))
        {
            return IdentityOperationResult.Failure(["Login failed"]);
        }
        var roles = await userManager.GetRolesAsync(user);

        var key = jwtProvider.GenerateToken(user.Id, user.UserName);
    }

    public async Task<IdentityOperationResult> RegisterAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new IdentityUser
        {
            UserName = username,
            Email = email
        };

        var result = await userManager.CreateAsync(user, password);
        //jwtProvider.GenerateToken()
        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(
                result.Errors.Select(error => error.Description));
    }
}
