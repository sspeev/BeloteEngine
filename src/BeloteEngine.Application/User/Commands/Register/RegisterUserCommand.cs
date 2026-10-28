using MediatR;

namespace BeloteEngine.Application.User.Commands.Register;

public sealed record RegisterUserCommand(
    string Username,
    string Email,
    string Password) : IRequest<RegisterUserResult>;

public sealed record RegisterUserResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiresAt,
    DateTime? RefreshTokenExpiresAt,
    IReadOnlyCollection<string> Errors)
{
    public static RegisterUserResult Success(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt) =>
        new(true, accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt, []);

    public static RegisterUserResult Failure(IEnumerable<string> errors) =>
        new(false, null, null, null, null, [..errors]);
}
