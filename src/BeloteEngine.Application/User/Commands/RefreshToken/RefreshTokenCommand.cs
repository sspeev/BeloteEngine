using MediatR;

namespace BeloteEngine.Application.User.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress) : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiresAt,
    DateTime? RefreshTokenExpiresAt,
    IReadOnlyCollection<string> Errors)
{
    public static RefreshTokenResult Success(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt) =>
        new(true, accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt, []);

    public static RefreshTokenResult Failure(IEnumerable<string> errors) =>
        new(false, null, null, null, null, [.. errors]);
}