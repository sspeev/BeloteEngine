using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Application.DTOs;

namespace BeloteEngine.Application.Contracts;

public interface ILobbyService
{
    Lobby CreateLobby(string lobbyName);
    Lobby CreateLobby(string lobbyName, string ipAddress);
    JoinResult JoinLobby(Player player);
    bool LeaveLobby(Player player, int lobbyId);
    Lobby GetLobby(int lobbyId);
    List<LobbyInfoModel> GetAvailableLobbies();
    bool IsFull(int lobbyId);
    void ResetLobby(int lobbyId);
    (Player? player, Lobby? lobby) RemovePlayerByConnectionId(string connectionId);
}