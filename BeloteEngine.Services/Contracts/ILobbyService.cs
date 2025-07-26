using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface ILobbyService
    {
        //public ILobby CreateLobby();
        public Task<JoinResult> JoinLobby(Player player);
        public Task<bool> LeaveLobby(Player player);
        //Task<LobbyInfo> GetLobbyInfo();
        //Task<bool> StartGame();
        //public Task HandlePlayerDisconnection(Player player);
        public Task NotifyLobbyUpdate();
        public void ResetLobby();
    }
}