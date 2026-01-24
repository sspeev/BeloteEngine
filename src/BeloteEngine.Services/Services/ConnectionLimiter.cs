using BeloteEngine.Services.Contracts;
using System.Collections.Concurrent;

namespace BeloteEngine.Services.Services;

public class ConnectionLimiter : IConnectionLimiter
{
    private const int MAX_CONNECTIONS_PER_IP = 4; // One per player slot
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionsByIp = new();
    private readonly object _lock = new();

    public bool CanConnect(string ipAddress)
    {
        lock (_lock)
        {
            if (!_connectionsByIp.TryGetValue(ipAddress, out var connections))
                return true;

            return connections.Count < MAX_CONNECTIONS_PER_IP;
        }
    }

    public void TrackConnection(string ipAddress, string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionsByIp.TryGetValue(ipAddress, out var connections))
            {
                connections = new HashSet<string>();
                _connectionsByIp[ipAddress] = connections;
            }
            connections.Add(connectionId);
        }
    }

    public void RemoveConnection(string ipAddress, string connectionId)
    {
        lock (_lock)
        {
            if (_connectionsByIp.TryGetValue(ipAddress, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    _connectionsByIp.TryRemove(ipAddress, out _);
                }
            }
        }
    }
}