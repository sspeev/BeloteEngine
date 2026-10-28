using MediatR;

namespace BeloteEngine.Application.User.Commands.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password) : IRequest<LoginUserResult>;

public sealed record LoginUserResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiresAt,
    DateTime? RefreshTokenExpiresAt,
    IReadOnlyCollection<string> Errors)
{
    public static LoginUserResult Success(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt) =>
        new(true, accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt, []);

    public static LoginUserResult Failure(IEnumerable<string> errors) =>
        new(false, null, null, null, null, [.. errors]);
}