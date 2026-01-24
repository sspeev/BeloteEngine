namespace BeloteEngine.Services.Contracts;

public interface IConnectionLimiter
{
    bool CanConnect(string ipAddress);
    void TrackConnection(string ipAddress, string connectionId);
    void RemoveConnection(string ipAddress, string connectionId);
}
