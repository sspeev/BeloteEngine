using MediatR;

namespace BeloteEngine.Application.User.Commands.Register;

public sealed record RegisterUserCommand(
    string Username,
    string Email,
    string Password) : IRequest<RegisterUserResult>;

public sealed record RegisterUserResult(
    bool Succeeded,
    string? Token,
    IReadOnlyCollection<string> Errors)
{
    public static RegisterUserResult Success(string token) =>
        new(true, token, []);

    public static RegisterUserResult Failure(IEnumerable<string> errors) =>
        new(false, null, [..errors]);
}
