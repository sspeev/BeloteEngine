using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.User.Commands.RevokeToken;
using MediatR;

namespace BeloteEngine.Application.User.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler(
    IUserIdentityService userIdentityService)
    : IRequestHandler<RevokeTokenCommand, bool>
{
    public async Task<bool> Handle(
        RevokeTokenCommand request,
        CancellationToken cancellationToken)
    {
        return await userIdentityService.RevokeTokenAsync(
            request.RefreshToken,
            request.IpAddress,
            cancellationToken: cancellationToken);
    }
}