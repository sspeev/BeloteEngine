using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface ILobby
    {
        List<Player> ConnectedPlayers { get; }
        bool GameStarted { get; set; }
        DateTime CreatedAt { get; }
        object LobbyLock { get; }

        Game Game { get; set; }

        // You could add methods here if needed
        void Reset();
        bool IsFull();
    }
}
