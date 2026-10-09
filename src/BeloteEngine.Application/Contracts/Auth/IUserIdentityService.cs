using BeloteEngine.Application.DTOs.Auth;

namespace BeloteEngine.Application.Contracts.Auth;

public interface IUserIdentityService
{
    Task<IdentityOperationResult> RegisterAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<IdentityOperationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
