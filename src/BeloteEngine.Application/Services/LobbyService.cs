using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.DTOs;
using BeloteEngine.Application.Security;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Domain.Entities.Models;
using Microsoft.Extensions.Logging;
using static System.StringComparison;
using static BeloteEngine.Application.Constants.LobbyConstants;
using static BeloteEngine.Domain.Entities.Enums.Status;

namespace BeloteEngine.Application.Services;

public class LobbyService(
    IGameService _gameService
    , ILobbyJoinValidator _joinValidator
    , ILobbyStore _lobbyStore
    , ILogger<LobbyService> _logger
    , CachingService _cachingService) : ILobbyService
{
    private readonly Lock _lockService = new();
    private readonly Lock _cleanupTimerLock = new();
    private Timer? _cleanupTimer;

    public Lobby CreateLobby(string lobbyName, Player creator)
    {
        EnsureCleanupTimerStarted();
        lobbyName = InputValidator.SanitizeLobbyName(lobbyName);
        lock (_lockService)
        {
            if (_lobbyStore.Count >= MAX_TOTAL_LOBBIES)
                throw new InvalidOperationException("Server is full. Please try again later.");

            if (creator.LobbyId != 0)
                throw new InvalidOperationException("Cannot host lobby while being in another!");

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

                if (!_lobbyStore.TryAdd(lobbyId, lobby))
                {
                    continue;
                }
                _cachingService.Remove($"{lobbyId}");

                _logger.LogInformation("Created lobby {LobbyId} '{LobbyName}' from player {}",
                    lobbyId, lobbyName, creator.Name);

                return lobby;
            }
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
                //OnLobbyRemoved(lobbyId);
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

    public JoinResult JoinLobby(int lobbyId, Player player)
    {
        EnsureCleanupTimerStarted();

        lock (_lockService)
        {
            if (lobbyId == 0)
            {
                return Failure("Invalid lobby ID.");
            }

            if (!_lobbyStore.TryGet(lobbyId, out var lobby))
            {
                return Failure($"Lobby {lobbyId} does not exist.");
            }

            var validation = _joinValidator.Validate(lobby, player);

            if (!validation.IsValid)
            {
                return Failure(validation.ErrorMessage!);
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

    private static string? ValidateJoin(Lobby lobby, Player player)
    {
        CompactPlayers(lobby.ConnectedPlayers);

        if (NonNullCount(lobby.ConnectedPlayers) >= 4)
        {
            return "Lobby is full.";
        }

        var existingPlayer = lobby.ConnectedPlayers.FirstOrDefault(existing =>
            string.Equals(existing.UserId, player.UserId, OrdinalIgnoreCase));

        if (existingPlayer is not null && existingPlayer.LobbyId != 0)
        {
            return "Player name is already in use.";
        }

        return null;
    }

    private static JoinResult Failure(string errorMessage)
    {
        return new JoinResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    public bool LeaveLobby(Player player, int lobbyId)
    {
        EnsureCleanupTimerStarted();
        if (!_lobbyStore.TryGet(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lockService)
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
                if (_lobbyStore.TryRemove(lobbyId, out _))
                {
                    //OnLobbyRemoved(lobbyId);
                    _logger.LogInformation("Removed empty lobby {LobbyId} immediately on player leave.", lobbyId);
                }
            }

            return removed > 0;
        }
    }

    public Lobby GetLobby(int lobbyId)
    {
        EnsureCleanupTimerStarted();
        _lobbyStore.TryGet(lobbyId, out var lobby);
        return lobby;
    }

    public List<LobbyInfoModel> GetAvailableLobbies()
    {
        EnsureCleanupTimerStarted();
        return [.. _lobbyStore
            .GetAll()
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
        if (!_lobbyStore.TryGet(lobbyId, out var lobby))
            return false;

        CompactPlayers(lobby.ConnectedPlayers);
        return NonNullCount(lobby.ConnectedPlayers) >= 4;
    }

    public void ResetLobby(int lobbyId)
    {
        EnsureCleanupTimerStarted();
        if (_lobbyStore.TryGet(lobbyId, out var lobby))
        {
            lock (_lockService)
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
    public (Player? player, Lobby? lobby) RemovePlayerByConnectionId(string connectionId)
    {
        lock (_lockService)
        {
            foreach (var lobby in _lobbyStore.GetAll())
            {
                var player = lobby.ConnectedPlayers.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (player != null)
                {
                    lobby.ConnectedPlayers.Remove(player);
                    lobby.UpdateActivity();
                    InvalidateLobbyCache(lobby.Id);

                    if (lobby.ConnectedPlayers.Count == 0)
                    {
                        if (_lobbyStore.TryRemove(lobby.Id, out _))
                        {
                            //OnLobbyRemoved(lobby.Id);
                            _logger.LogInformation("Removed empty lobby {LobbyId} after connection loss.", lobby.Id);
                            return (player, null); // Lobby is gone
                        }
                    }
                    return (player, lobby);
                }
            }
        }
        return (null, null);
    }
}
