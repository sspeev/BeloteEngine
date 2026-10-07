using BeloteEngine.Application.DTOs;
using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.Contracts.Lobby;

public interface ILobbyCreationValidator
{
    LobbyCreationValidation Validate(int currentLobbyCount, Player creator);
}