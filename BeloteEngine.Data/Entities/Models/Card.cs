using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Card(Suit suit, string rank, int points)
    {
        public Suit Suit { get; set; } = suit;
        public string Rank { get; set; } = rank;
        public int Points { get; set; } = points;
    }
}
