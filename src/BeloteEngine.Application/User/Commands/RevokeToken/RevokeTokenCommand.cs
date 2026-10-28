using MediatR;

namespace BeloteEngine.Application.User.Commands.RevokeToken;

public sealed record RevokeTokenCommand(
    string RefreshToken,
    string IpAddress) : IRequest<bool>;