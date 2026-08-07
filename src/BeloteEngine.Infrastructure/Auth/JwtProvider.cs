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
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        
    }
}