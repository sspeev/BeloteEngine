using BeloteEngine.Application.DTOs;
using BeloteEngine.Domain.Entities.Models;
using LobbyModel = BeloteEngine.Domain.Entities.Models.Lobby;
namespace BeloteEngine.Application.Contracts.Lobby;

public interface ILobbyJoinValidator
{
    LobbyJoinValidation Validate(LobbyModel lobby, Player player);
}