namespace BeloteEngine.Application.DTOs.Auth;

public sealed record IdentityOperationResult(bool Succeeded, string? AccessToken, string? RefreshToken, DateTime? AccessTokenExpiresAt, DateTime? RefreshTokenExpiresAt, IReadOnlyCollection<string> Errors)
{
    public static IdentityOperationResult Success(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt) =>
        new(true, accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt, []);

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new(false, null, null, null, null, [.. errors]);
}
