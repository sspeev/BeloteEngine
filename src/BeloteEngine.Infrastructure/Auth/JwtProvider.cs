using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BeloteEngine.Application.Contracts.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BeloteEngine.Infrastructure.Auth;

public sealed class JwtProvider(IConfiguration configuration) : IJwtProvider
{
    private readonly string _secretKey = configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "BeloteEngineAPI";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "BeloteClientApp";
    private readonly TimeSpan _accessTokenLifetime = TimeSpan.FromHours(2);
    private readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(7);

    public string GenerateAccessToken(string userId, string username)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_accessTokenLifetime),
            SigningCredentials = credentials,
            Issuer = _issuer,
            Audience = _audience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }

    public TokenPair GenerateTokenPair(string userId, string username)
    {
        var accessToken = GenerateAccessToken(userId, username);
        var refreshToken = GenerateRefreshToken();
        var accessTokenExpiresAt = DateTime.UtcNow.Add(_accessTokenLifetime);
        var refreshTokenExpiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime);

        return new TokenPair(accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt);
    }
}
