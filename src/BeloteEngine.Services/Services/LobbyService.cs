using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Models;
using BeloteEngine.Services.Security;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static System.StringComparison;
using static BeloteEngine.Data.Entities.Enums.Status;
using static BeloteEngine.Services.Constants.LobbyConstants;

namespace BeloteEngine.Services.Services;

public class LobbyService(
    IGameService _gameService
    , ILogger<LobbyService> _logger
    , CachingService _cachingService) : ILobbyService
{
    private readonly ConcurrentDictionary<int, Lobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, int> _lobbyCountByIp = new();
    private readonly ConcurrentDictionary<int, string> _lobbyToIp = new();
    private readonly object _lockObject = new();
    private readonly object _cleanupTimerLock = new();
    private Timer? _cleanupTimer;

    public Lobby CreateLobby(string lobbyName, string ipAddress)
    {
        EnsureCleanupTimerStarted();
        lobbyName = InputValidator.SanitizeLobbyName(lobbyName);
        lock (_lockObject)
        {
            if (_lobbies.Count >= MAX_TOTAL_LOBBIES)
            {
                throw new InvalidOperationException("Server is full. Please try again later.");
            }

            if (!_lobbyCountByIp.TryGetValue(ipAddress, out var currentCount))
            {
                currentCount = 0;
            }

            if (currentCount >= MAX_LOBBIES_PER_IP)
            {
                throw new InvalidOperationException(
                    $"You can only create {MAX_LOBBIES_PER_IP} lobbies at a time.");
            }

            var lobby = new Lobby
            {
                Game = _gameService.Creator(),
                Name = lobbyName,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            while (true)
            {
                var lobbyId = Random.Shared.Next(1000, 9999);
                lobby.Id = lobbyId;

                if (!_lobbies.TryAdd(lobbyId, lobby))
                {
                    continue;
                }

                _lobbyToIp[lobbyId] = ipAddress;
                _lobbyCountByIp[ipAddress] = currentCount + 1;
                _cachingService.Remove($"{lobbyId}");

                _logger.LogInformation("Created lobby {LobbyId} '{LobbyName}' from IP {IpAddress}",
                    lobbyId, lobbyName, ipAddress);

                return lobby;
            }
        }
    }

    // Overload for backward compatibility
    public Lobby CreateLobby(string lobbyName)
    {
        EnsureCleanupTimerStarted();
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
        EnsureCleanupTimerStarted();
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

            var existingPlayer = lobby.ConnectedPlayers.FirstOrDefault(p => string.Equals(p.Name, player.Name, OrdinalIgnoreCase));
            if (existingPlayer != null)
            {
                if (!string.IsNullOrWhiteSpace(existingPlayer.SessionId) &&
                    !string.Equals(existingPlayer.SessionId, player.SessionId, Ordinal))
                {
                    return new JoinResult
                    {
                        Success = false,
                        ErrorMessage = "Player name is already in use."
                    };
                }

                existingPlayer.ConnectionId = player.ConnectionId;
                existingPlayer.SessionId = player.SessionId;
                existingPlayer.Status = Connected;
                lobby.UpdateActivity();
                InvalidateLobbyCache(lobbyId);

                return new JoinResult
                {
                    Success = true,
                    Lobby = lobby
                };
            }

            if (IsFull(lobbyId))
            {
                return new JoinResult
                {
                    Success = false,
                    ErrorMessage = "Lobby is full."
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
        EnsureCleanupTimerStarted();
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

            if (lobby.ConnectedPlayers.Count == 0)
            {
                if (_lobbies.TryRemove(lobbyId, out _))
                {
                    OnLobbyRemoved(lobbyId);
                    _logger.LogInformation("Removed empty lobby {LobbyId} immediately on player leave.", lobbyId);
                }
            }

            return removed > 0;
        }
    }

    public Lobby GetLobby(int lobbyId)
    {
        EnsureCleanupTimerStarted();
        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

    public List<LobbyInfoModel> GetAvailableLobbies()
    {
        EnsureCleanupTimerStarted();
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
        EnsureCleanupTimerStarted();
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            return false;

        CompactPlayers(lobby.ConnectedPlayers);
        return NonNullCount(lobby.ConnectedPlayers) >= 4;
    }

    public void ResetLobby(int lobbyId)
    {
        EnsureCleanupTimerStarted();
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

    private void EnsureCleanupTimerStarted()
    {
        if (_cleanupTimer is not null)
            return;

        lock (_cleanupTimerLock)
        {
            _cleanupTimer ??= new Timer(
                _ => CleanupAbandonedLobbies(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));
        }
    }
}
