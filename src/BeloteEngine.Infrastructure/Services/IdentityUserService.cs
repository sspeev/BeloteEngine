using BeloteEngine.Application.Contracts;
using Microsoft.AspNetCore.Identity;

namespace BeloteEngine.Infrastructure.Services;

public sealed class IdentityUserService(
    UserManager<IdentityUser> userManager) : IUserIdentityService
{
    public async Task<IdentityOperationResult> CreateAsync(
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

        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(
                result.Errors.Select(error => error.Description));
    }
}
