using BeloteEngine.Application.Contracts.Auth;
using MediatR;

namespace BeloteEngine.Application.User.Commands.Login;

public sealed class LoginUserCommandHandler(
    IUserIdentityService userIdentityService)
    : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = await userIdentityService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        return result.Succeeded
            ? LoginUserResult.Success(result.Token!)
            : LoginUserResult.Failure(result.Errors);
    }
}