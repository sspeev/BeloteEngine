using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;

namespace BeloteEngine.Services.Services
{
    public class Lobby : ILobby
    {
        public List<Player> ConnectedPlayers { get; } = new();
        public bool GameStarted { get; set; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public object LobbyLock { get; } = new();

        public void Reset()
        {
            lock (LobbyLock)
            {
                ConnectedPlayers.Clear();
                GameStarted = false;
            }
        }

        public bool IsFull() => ConnectedPlayers.Count >= 4;
    }
}
