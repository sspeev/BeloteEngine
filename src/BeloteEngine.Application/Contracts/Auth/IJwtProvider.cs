namespace BeloteEngine.Application.Contracts.Auth;

public interface IJwtProvider
{
    string GenerateToken(string userId, string username);
}
