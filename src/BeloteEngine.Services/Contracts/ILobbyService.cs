using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Models;

namespace BeloteEngine.Services.Contracts;

public interface ILobbyService
{
    Lobby CreateLobby(string lobbyName);
    JoinResult JoinLobby(Player player);
    bool LeaveLobby(Player player, int lobbyId);
    Lobby GetLobby(int lobbyId);
    List<LobbyInfoModel> GetAvailableLobbies();
    bool IsFull(int lobbyId);
    void ResetLobby(int lobbyId);
}