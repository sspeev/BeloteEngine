using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface ILobbyService
    {
        public Lobby CreateLobby(string lobbyName);
        public JoinResult JoinLobby(Player player);
        public bool LeaveLobby(Player player, int lobbyId);
        public Lobby GetLobby(int lobbyId);
        //Task<bool> StartGame();
        //public Task HandlePlayerDisconnection(Player player);
        public Task NotifyLobbyUpdate(int lobbyId);
        public bool IsFull(int lobbyId);
        public void ResetLobby(int lobbyId);
    }
}