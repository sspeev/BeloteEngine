namespace BeloteEngine.Data.Entities.Models
{
    public class Team
    {
        public Player[] Players { get; init; } = new Player[2];

        public int Score { get; set; }
    }
}
