
namespace BeloteEngine.Application.Contracts;

public interface IJwtProvider
{
    string GenerateToken(string userId, string username);
}
