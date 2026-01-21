namespace BeloteEngine.Services.Models
{
    //this should be somewhere else
    public class LobbyInfoModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public bool IsFull { get; set; }
        public bool GameStarted { get; set; }
        public string GamePhase { get; set; } = string.Empty;
    }
}