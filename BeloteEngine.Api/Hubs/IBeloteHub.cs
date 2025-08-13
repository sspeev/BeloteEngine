namespace BeloteEngine.Api.Hubs
{
    public interface IBeloteHub
    {
        Task JoinLobby(int lobbyId, string playerName);
        Task LeaveLobby(int lobbyId, string playerName);
    }
}
