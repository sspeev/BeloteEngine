namespace BeloteEngine.Application.Contracts;

public interface IConnectionRegistry
{
    void Register(string userId, string connectionId);
    void Unregister(string connectionId);
    string? GetConnectionId(string userId);
}