using MediatR;

namespace BeloteEngine.Application.User.Commands.Create;

public sealed record CreateUserCommand(
    string Username,
    string Email,
    string Password) : IRequest<CreateUserResult>;

public sealed record CreateUserResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors)
{
    public static CreateUserResult Success() =>
        new(true, []);

    public static CreateUserResult Failure(IEnumerable<string> errors) =>
        new(false, [..errors]);
}