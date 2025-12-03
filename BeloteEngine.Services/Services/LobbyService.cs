using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static BeloteEngine.Data.Entities.Enums.Status;
using static System.StringComparison;

namespace BeloteEngine.Services.Services;

public class LobbyService(
    IGameService gameService
    , ILogger<LobbyService> logger)
    : ILobbyService
{
    private readonly IGameService _gameService = gameService;
    private readonly ILogger<LobbyService> _logger = logger;
    private readonly ConcurrentDictionary<int, Lobby> _lobbies = new();
    private readonly object _lockObject = new();

    public Lobby CreateLobby(string lobbyName)
    {
        var lobby = new Lobby
        {
            Game = _gameService.Creator()
        };
        int lobbyId;
        do {
            lobbyId = new Random().Next(1000, 9999);
        } while (_lobbies.ContainsKey(lobbyId));
            
        lobby.Id = lobbyId;
        lobby.Name = lobbyName;
        _lobbies.TryAdd(lobbyId, lobby);
            
        _logger.LogInformation("New lobby created with ID {LobbyId}", lobbyId);
        return lobby;
    }

    private static void CompactPlayers(List<Player> players)
    {
        players.RemoveAll(_ => false);
    }

    private static int NonNullCount(List<Player> players) => players.Count(_ => true);

    public JoinResult JoinLobby(Player player)
    {
        var lobbyId = player.LobbyId ?? 0;
        if (lobbyId == 0)
        {
            return new JoinResult
            {
                Success = false,
                ErrorMessage = "Invalid lobby ID."
            };
        }
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return new JoinResult
            {
                Success = false,
                ErrorMessage = $"Lobby {lobbyId} does not exist."
            };
        }
            
        lock (_lockObject)
        {
            CompactPlayers(lobby.ConnectedPlayers);

            if (IsFull(lobbyId))
            {
                return new JoinResult
                {
                    Success = false,
                    ErrorMessage = "Lobby is full."
                };
            }
                
            if (lobby.ConnectedPlayers.Any(p => string.Equals(p.Name, player.Name, OrdinalIgnoreCase)))
            {
                return new JoinResult
                {
                    Success = false,
                    ErrorMessage = "Player already connected."
                };
            }
                
            lobby.ConnectedPlayers.Add(player);
            player.Status = Connected;
            player.LobbyId = lobbyId;
                
            return new JoinResult 
            { 
                Success = true,
                Lobby = lobby
            };
        }
    }

    public bool LeaveLobby(Player player, int lobbyId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lockObject)
        {
            // Remove by stable key (Name) and clean up any nulls left behind
            var removed = lobby.ConnectedPlayers.RemoveAll(p =>
                string.Equals(p.Name, player.Name, OrdinalIgnoreCase));

            // As a fallback, remove by reference if nothing matched
            if (removed == 0)
            {
                removed = lobby.ConnectedPlayers.RemoveAll(p => ReferenceEquals(p, player));
            }

            CompactPlayers(lobby.ConnectedPlayers);

            if (removed == 0)
            {
                return false;
            }
                
            if (NonNullCount(lobby.ConnectedPlayers) < 4)
            {
                lobby.GameStarted = false;
            }
                
            if (NonNullCount(lobby.ConnectedPlayers) == 0)
            {
                _lobbies.TryRemove(lobbyId, out _);
                _logger.LogInformation("Lobby {LobbyId} removed as it's empty", lobbyId);
            }
                
            return true;
        }
    }

    public Task NotifyLobbyUpdate(int lobbyId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return Task.CompletedTask;
        CompactPlayers(lobby.ConnectedPlayers);
        _logger.LogInformation("Lobby {LobbyId} updated. Players: {PlayerCount}, Game started: {GameStarted}",
            lobbyId, NonNullCount(lobby.ConnectedPlayers), lobby.GameStarted);
        return Task.CompletedTask;
    }

    public void ResetLobby(int lobbyId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return;
        lock (_lockObject)
        {
            lobby.ConnectedPlayers.Clear();
            lobby.GameStarted = false;
            lobby.Game = _gameService.Creator();
        }
    }

    public bool IsFull(int lobbyId)
    {
        var lobby = _lobbies[lobbyId];
        CompactPlayers(lobby.ConnectedPlayers);
        return NonNullCount(lobby.ConnectedPlayers) >= 4;
    }

    public Lobby GetLobby(int lobbyId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            CompactPlayers(lobby.ConnectedPlayers);
            return lobby;
        }

        _logger.LogWarning("Lobby {LobbyId} not found", lobbyId);
        return null!;
    }

    public List<LobbyInfo> GetAvailableLobbies()
    {
        return [.. _lobbies.Values
            .Select(l =>
            {
                CompactPlayers(l.ConnectedPlayers);
                return l;
            })
            .Where(l => !l.GameStarted && NonNullCount(l.ConnectedPlayers) < 4)
            .Select(l => new LobbyInfo
            {
                Id = l.Id,
                Name = l.Name,
                PlayerCount = NonNullCount(l.ConnectedPlayers),
                IsFull = NonNullCount(l.ConnectedPlayers) >= 4,
                GameStarted = l.GameStarted,
                GamePhase = l.GamePhase
                    
            })];
    }
}