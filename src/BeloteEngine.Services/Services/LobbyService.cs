using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Models;
using BeloteEngine.Services.Security;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static BeloteEngine.Data.Entities.Enums.Status;
using static System.StringComparison;

namespace BeloteEngine.Services.Services;

public class LobbyService : ILobbyService
{
    private readonly IGameService _gameService;
    private readonly ILogger<LobbyService> _logger;
    private readonly CachingService _cachingService;
    private readonly ConcurrentDictionary<int, Lobby> _lobbies = new();
    private readonly object _lockObject = new();

    private readonly ConcurrentDictionary<string, int> _lobbyCountByIp = new();
    private readonly ConcurrentDictionary<int, string> _lobbyToIp = new();

    private const int MAX_TOTAL_LOBBIES = 100;
    private const int MAX_LOBBIES_PER_IP = 5;
    private readonly Timer _cleanupTimer;

    public LobbyService(
        IGameService gameService,
        ILogger<LobbyService> logger,
        CachingService cachingService)
    {
        _gameService = gameService;
        _logger = logger;
        _cachingService = cachingService;

        // Start cleanup timer (every 5 minutes)
        _cleanupTimer = new Timer(
            _ => CleanupAbandonedLobbies(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    public Lobby CreateLobby(string lobbyName, string ipAddress)
    {
        lobbyName = InputValidator.SanitizeLobbyName(lobbyName);
        if (_lobbies.Count >= MAX_TOTAL_LOBBIES)
        {
            throw new InvalidOperationException("Server is full. Please try again later.");
        }

        // Per-IP limit
        var currentCount = _lobbyCountByIp.GetOrAdd(ipAddress, 0);
        if (currentCount >= MAX_LOBBIES_PER_IP)
        {
            throw new InvalidOperationException(
                $"You can only create {MAX_LOBBIES_PER_IP} lobbies at a time.");
        }

        // Create lobby
        var lobby = new Lobby
        {
            Game = _gameService.Creator(),
            Name = lobbyName,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        // Generate unique ID
        int lobbyId;
        do
        {
            lobbyId = Random.Shared.Next(1000, 9999);
        } while (_lobbies.ContainsKey(lobbyId));

        lobby.Id = lobbyId;
        _lobbies.TryAdd(lobbyId, lobby);
        _lobbyToIp[lobbyId] = ipAddress;

        // Increment count for this IP
        _lobbyCountByIp.AddOrUpdate(ipAddress, 1, (key, count) => count + 1);

        // Cache the new lobby
        var cacheKey = $"{lobbyId}";
        _cachingService.Remove(cacheKey);

        _logger.LogInformation("Created lobby {LobbyId} '{LobbyName}' from IP {IpAddress}",
            lobbyId, lobbyName, ipAddress);

        return lobby;
    }

    // Overload for backward compatibility
    public Lobby CreateLobby(string lobbyName)
    {
        return CreateLobby(lobbyName, "unknown");
    }

    private void OnLobbyRemoved(int lobbyId)
    {
        if (_lobbyToIp.TryRemove(lobbyId, out var ipAddress))
        {
            _lobbyCountByIp.AddOrUpdate(ipAddress, 0, (key, count) => Math.Max(0, count - 1));
        }
    }

    private void CleanupAbandonedLobbies()
    {
        var now = DateTime.UtcNow;
        var lobbiestoRemove = _lobbies.Values
            .Where(l =>
                l.ConnectedPlayers.Count == 0 ||
                (now - l.LastActivity) > TimeSpan.FromMinutes(30))
            .Select(l => l.Id)
            .ToList();

        foreach (var lobbyId in lobbiestoRemove)
        {
            if (_lobbies.TryRemove(lobbyId, out _))
            {
                OnLobbyRemoved(lobbyId);
                _logger.LogInformation("Cleaned up abandoned lobby {LobbyId}", lobbyId);
            }
        }

        if (lobbiestoRemove.Any())
        {
            _logger.LogInformation("Cleanup: Removed {Count} abandoned lobbies", lobbiestoRemove.Count);
        }
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
            lobby.UpdateActivity();

            InvalidateLobbyCache(lobbyId);

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
            var removed = lobby.ConnectedPlayers.RemoveAll(p =>
                string.Equals(p.Name, player.Name, OrdinalIgnoreCase));

            if (removed == 0)
            {
                removed = lobby.ConnectedPlayers.RemoveAll(p => ReferenceEquals(p, player));
            }

            CompactPlayers(lobby.ConnectedPlayers);
            lobby.UpdateActivity();
            InvalidateLobbyCache(lobbyId);

            return removed > 0;
        }
    }

    public Lobby GetLobby(int lobbyId)
    {
        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

    public List<LobbyInfoModel> GetAvailableLobbies()
    {
        return [.. _lobbies.Values
            .Select(l =>
            {
                CompactPlayers(l.ConnectedPlayers);
                return l;
            })
            .Where(l => !l.GameStarted && NonNullCount(l.ConnectedPlayers) < 4)
            .Select(l => new LobbyInfoModel
            {
                Id = l.Id,
                Name = l.Name,
                PlayerCount = NonNullCount(l.ConnectedPlayers),
                IsFull = NonNullCount(l.ConnectedPlayers) >= 4,
                GameStarted = l.GameStarted,
                GamePhase = l.GamePhase
            })];
    }

    public bool IsFull(int lobbyId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            return false;

        CompactPlayers(lobby.ConnectedPlayers);
        return NonNullCount(lobby.ConnectedPlayers) >= 4;
    }

    public void ResetLobby(int lobbyId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            lock (_lockObject)
            {
                lobby.ConnectedPlayers.Clear();
                lobby.GameStarted = false;
                lobby.GamePhase = "waiting";
                lobby.Game = _gameService.Creator();
                lobby.UpdateActivity();
                InvalidateLobbyCache(lobbyId);
            }
        }
    }

    private void InvalidateLobbyCache(int lobbyId)
    {
        var cacheKey = $"{lobbyId}";
        _cachingService.Remove(cacheKey);
    }
}