using MediatR;

namespace BeloteEngine.Application.User.Commands.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password) : IRequest<LoginUserResult>;

public sealed record LoginUserResult(
    bool Succeeded,
    string? Token,
    IReadOnlyCollection<string> Errors)
{
    public static LoginUserResult Success(string token) =>
        new(true, token, []);

    public static LoginUserResult Failure(IEnumerable<string> errors) =>
        new(false, null, [.. errors]);
}