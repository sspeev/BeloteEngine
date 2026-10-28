using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.DTOs.Auth;
using BeloteEngine.Domain.Entities;
using BeloteEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeloteEngine.Infrastructure.Auth;

public sealed class IdentityUserService(
    UserManager<IdentityUser> userManager,
    IJwtProvider jwtProvider,
    BeloteEngineDbContext dbContext,
    ILogger<IdentityUserService> logger) : IUserIdentityService
{
    private readonly ILogger<IdentityUserService> _logger = logger;
    public async Task<IdentityUserInfo?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId);

        return user is null
            ? null
            : new IdentityUserInfo(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email);
    }

    public async Task<IdentityOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null ||
            !await userManager.CheckPasswordAsync(user, password))
        {
            return IdentityOperationResult.Failure(["Login failed"]);
        }

        await RevokeAllUserTokensAsync(user.Id, "new-login", cancellationToken);

        var tokenPair = jwtProvider.GenerateTokenPair(user.Id, user.UserName!);
        await StoreRefreshTokenAsync(user.Id, tokenPair.RefreshToken, tokenPair.RefreshTokenExpiresAt, null, cancellationToken);

        return IdentityOperationResult.Success(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.AccessTokenExpiresAt,
            tokenPair.RefreshTokenExpiresAt);
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

        var tokenPair = jwtProvider.GenerateTokenPair(user.Id, user.UserName!);
        await StoreRefreshTokenAsync(user.Id, tokenPair.RefreshToken, tokenPair.RefreshTokenExpiresAt, null, cancellationToken);

        return IdentityOperationResult.Success(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.AccessTokenExpiresAt,
            tokenPair.RefreshTokenExpiresAt);
    }

    public async Task<IdentityOperationResult> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken)
    {

        var tokenHash = jwtProvider.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return IdentityOperationResult.Failure(["Invalid or expired refresh token"]);
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId);
        if (user is null)
        {
            return IdentityOperationResult.Failure(["User not found"]);
        }

        // Soft IP check — log anomaly but don't block (mobile users change IPs)
        if (storedToken.CreatedByIp != ipAddress)
        {
            _logger?.LogWarning("Refresh token used from different IP. User: {UserId}, Old: {OldIp}, New: {NewIp}",
                storedToken.UserId, storedToken.CreatedByIp, ipAddress);
        }

        // Revoke the old refresh token (rotation)
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        // Generate new token pair
        var newTokenPair = jwtProvider.GenerateTokenPair(user.Id, user.UserName!);
        storedToken.ReplacedByTokenHash = jwtProvider.HashRefreshToken(newTokenPair.RefreshToken);

        // Store new refresh token
        await StoreRefreshTokenAsync(user.Id, newTokenPair.RefreshToken, newTokenPair.RefreshTokenExpiresAt, ipAddress, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return IdentityOperationResult.Success(
            newTokenPair.AccessToken,
            newTokenPair.RefreshToken,
            newTokenPair.AccessTokenExpiresAt,
            newTokenPair.RefreshTokenExpiresAt);
    }

    public async Task<bool> RevokeTokenAsync(
        string refreshToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = jwtProvider.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return false;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task StoreRefreshTokenAsync(
        string userId,
        string refreshToken,
        DateTime expiresAt,
        string? createdByIp,
        CancellationToken cancellationToken)
    {
        var existingTokens = await dbContext.RefreshTokens
        .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
        .ToListAsync(cancellationToken);

        foreach (var t in existingTokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedByIp = createdByIp ?? "replaced-by-new-login";
        }

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            TokenHash = jwtProvider.HashRefreshToken(refreshToken),
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken ct)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedByIp = reason;
        }
        await dbContext.SaveChangesAsync(ct);
    }
}
