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

    Task<IdentityOperationResult> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken);

    Task<bool> RevokeTokenAsync(
        string refreshToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IdentityUserInfo?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken);
}
