using BeloteEngine.Application.Contracts;
using MediatR;

namespace BeloteEngine.Application.User.Commands.Create;

public sealed class CreateUserCommandHandler(
    IUserIdentityService userIdentityService)
    : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    public async Task<CreateUserResult> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = await userIdentityService.CreateAsync(
            request.Username,
            request.Email,
            request.Password,
            cancellationToken);

        return result.Succeeded
            ? CreateUserResult.Success()
            : CreateUserResult.Failure(result.Errors);
    }
}