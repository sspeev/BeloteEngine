using System.Collections.Concurrent;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.Services;

public sealed class InMemoryLobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<int, Lobby> _lobbies = new();

    public int Count => _lobbies.Count;

    public bool TryGet(int lobbyId, out Lobby? lobby)
    {
        return _lobbies.TryGetValue(lobbyId, out lobby);
    }

    public bool TryAdd(int lobbyId, Lobby lobby)
    {
        return _lobbies.TryAdd(lobbyId, lobby);
    }

    public bool TryRemove(int lobbyId, out Lobby? lobby)
    {
        return _lobbies.TryRemove(lobbyId, out lobby);
    }

    public IReadOnlyCollection<Lobby> GetAll()
    {
        return [.. _lobbies.Values];
    }
}