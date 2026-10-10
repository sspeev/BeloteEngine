using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Contracts.Auth;
using MediatR;

namespace BeloteEngine.Application.User.Commands.Register;

public sealed class CreateUserCommandHandler(
    IUserIdentityService userIdentityService)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = await userIdentityService.RegisterAsync(
            request.Username,
            request.Email,
            request.Password,
            cancellationToken);

        return result.Succeeded
            ? RegisterUserResult.Success(result.Token!)
            : RegisterUserResult.Failure(result.Errors);
    }
}
