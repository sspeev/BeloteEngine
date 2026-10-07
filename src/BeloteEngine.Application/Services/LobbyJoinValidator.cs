using LobbyModel = BeloteEngine.Domain.Entities.Models.Lobby;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Application.DTOs;
using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.Services;

public sealed class LobbyJoinValidator : ILobbyJoinValidator
{
    private const int MaxPlayers = 4;

    public LobbyJoinValidation Validate(LobbyModel lobby, Player player)
    {
        var existingPlayer = lobby.ConnectedPlayers.FirstOrDefault(existing =>
            string.Equals(existing.UserId, player.UserId, StringComparison.OrdinalIgnoreCase));

        if (existingPlayer is not null && existingPlayer.LobbyId != 0)
        {
            return LobbyJoinValidation.Invalid("Player name is already in use.");
        }

        if (lobby.ConnectedPlayers.Count >= MaxPlayers)
        {
            return LobbyJoinValidation.Invalid("Lobby is full.");
        }

        return LobbyJoinValidation.Valid();
    }
}