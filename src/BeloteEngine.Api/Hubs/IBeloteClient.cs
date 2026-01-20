using BeloteEngine.Api.Models;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Api.Hubs
{
    public interface IBeloteClient
    {
        Task JoinLobby(int lobbyId, RequestInfoModel requset);

        Task<DeleteModel> LeaveLobby(LeaveRequestModel request);

        Task DeleteLobby();

        Task StartGame(Lobby lobby);

        Task DealingCards(int lobbyId, string gamePhase);
    }
}
