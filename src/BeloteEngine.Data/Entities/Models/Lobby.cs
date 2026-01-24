namespace BeloteEngine.Data.Entities.Models
{
    public class Lobby
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Player> ConnectedPlayers { get; } = [];
        public bool GameStarted { get; set; }
        public Game Game { get; set; } = null!;
        public string GamePhase { get; set; } = "waiting";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }
    }
}
