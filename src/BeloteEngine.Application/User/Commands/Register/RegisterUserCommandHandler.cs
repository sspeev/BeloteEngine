using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.User.Commands.Register;
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
            ? RegisterUserResult.Success(result.AccessToken!, result.RefreshToken!, result.AccessTokenExpiresAt!.Value, result.RefreshTokenExpiresAt!.Value)
            : RegisterUserResult.Failure(result.Errors);
    }
}
