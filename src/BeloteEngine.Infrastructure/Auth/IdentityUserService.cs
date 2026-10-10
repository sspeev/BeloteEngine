using BeloteEngine.Application.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using BeloteEngine.Application.DTOs.Auth;

namespace BeloteEngine.Infrastructure.Auth;

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
        var token = jwtProvider.GenerateToken(user.Id, user.UserName!);
        return IdentityOperationResult.Success(token);
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
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(error => error.Description));
        }

        var token = jwtProvider.GenerateToken(user.Id, user.UserName!);
        return IdentityOperationResult.Success(token);
    }
}
