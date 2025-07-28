using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface ILobbyService
    {
        Lobby CreateLobby(string lobbyName);
        JoinResult JoinLobby(Player player);
        bool LeaveLobby(Player player, int lobbyId);
        Lobby GetLobby(int lobbyId);
        List<LobbyInfo> GetAvailableLobbies(); // Add this
        Task NotifyLobbyUpdate(int lobbyId);
        bool IsFull(int lobbyId);
        void ResetLobby(int lobbyId);
    }

    //this should be somewhere else
    public class LobbyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public bool IsFull { get; set; }
        public bool GameStarted { get; set; }
    }
}