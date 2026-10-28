namespace BeloteEngine.Application.Contracts.Auth;

public interface IJwtProvider
{
    string GenerateAccessToken(string userId, string username);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    TokenPair GenerateTokenPair(string userId, string username);
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt);
