using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeloteEngine.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BeloteEngine.Infrastructure.Auth;

public class JwtProvider(IConfiguration configuration) : IJwtProvider
{
    private readonly IConfiguration _configuration = configuration;

    public string GenerateToken(string userId, string username)
    {
        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.Name, username)
        };
        var secretKey = _configuration["Jwt:Secret"]!;
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(signingKey,
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = credentials,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        var accessToken = tokenHandler.WriteToken(securityToken);

        return accessToken;
    }
}