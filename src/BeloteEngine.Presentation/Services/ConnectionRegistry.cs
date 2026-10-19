using BeloteEngine.Application.Contracts;
using System.Collections.Concurrent;

namespace BeloteEngine.Presentation.Services;

public sealed class ConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public string? GetConnectionId(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return _connections.TryGetValue(userId, out var connectionId)
            ? connectionId
            : null;
    }

    public void Register(string userId, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        _connections[userId] = connectionId;
    }

    public void Unregister(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        foreach (var pair in _connections)
        {
            if (pair.Value == connectionId)
            {
                ((ICollection<KeyValuePair<string, string>>)_connections)
                    .Remove(pair);
            }
        }
    }
}