namespace BeloteEngine.Data.Entities.Models
{
    public class Lobby
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public List<Player> ConnectedPlayers { get; set; } = [];
        public bool GameStarted { get; set; } = false;
        public Game Game { get; set; } = null!;
    }
}
