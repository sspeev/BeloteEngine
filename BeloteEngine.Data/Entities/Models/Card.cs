namespace BeloteEngine.Data.Entities.Models
{
    public class Card
    {
        public string Suit { get; set; }
        public string Rank { get; set; }
        public int Points { get; set; }

        public Card(string suit, string rank, int points)
        {
            Suit = suit;
            Rank = rank;
            Points = points;
        }
    }
}
