using BeloteEngine.Application.Constants;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Application.DTOs;
using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.Services;

public sealed class LobbyCreationValidator : ILobbyCreationValidator
{
    public LobbyCreationValidation Validate(int currentLobbyCount, Player creator)
    {
        if (currentLobbyCount >= LobbyConstants.MAX_TOTAL_LOBBIES)
        {
            return LobbyCreationValidation.Invalid(
                "Server is full. Please try again later.");
        }

        if (creator.LobbyId != 0)
        {
            return LobbyCreationValidation.Invalid(
                "Cannot host lobby while being in another!");
        }

        return LobbyCreationValidation.Valid();
    }
}