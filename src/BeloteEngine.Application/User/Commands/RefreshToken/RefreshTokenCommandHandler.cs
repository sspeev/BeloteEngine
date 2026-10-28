using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.User.Commands.RefreshToken;
using MediatR;

namespace BeloteEngine.Application.User.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserIdentityService userIdentityService)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await userIdentityService.RefreshTokenAsync(
            request.RefreshToken,
            request.IpAddress,
            cancellationToken);

        return result.Succeeded
            ? RefreshTokenResult.Success(result.AccessToken!, result.RefreshToken!, result.AccessTokenExpiresAt!.Value, result.RefreshTokenExpiresAt!.Value)
            : RefreshTokenResult.Failure(result.Errors);
    }
}