using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Card(Suit suit, string rank, int value, int power)
    {
        public Suit Suit { get; set; } = suit;
        public string Rank { get; set; } = rank;

        public int Value { get; set; } = value;
        
        public int Power { get; set; } = power;
    }
}
