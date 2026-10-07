using LobbyModel = BeloteEngine.Domain.Entities.Models.Lobby;
using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Application.DTOs;

namespace BeloteEngine.Application.Contracts.Lobby;

public interface ILobbyService
{
    public LobbyModel CreateLobby(string lobbyName, Player creator);

    public JoinResult JoinLobby(int lobbyId, Player player);

    public bool LeaveLobby(Player player, int lobbyId);

    public LobbyModel GetLobby(int lobbyId);

    public List<LobbyInfoModel> GetAvailableLobbies();

    public bool IsFull(int lobbyId);

    public void ResetLobby(int lobbyId);

    public (Player? player, LobbyModel? lobby) RemovePlayerByConnectionId(string connectionId);
}
