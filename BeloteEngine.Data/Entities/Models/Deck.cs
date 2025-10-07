namespace BeloteEngine.Data.Entities.Models
{
    public class Deck
    {
        public Stack<Card> Cards { get; set; }

        public Deck()
        {
            var suits = new List<string> { "Спатия", "Каро", "Купа", "Пика" };
            var ranks = new List<(string Rank, int Points)>
            {
                ("7", 0),
                ("8", 0),
                ("9", 0),
                ("10", 10),
                ("J", 2),
                ("Q", 3),
                ("K", 4),
                ("A", 11)
            };

            var allCards = suits.SelectMany(suit =>
                ranks.Select(rank => new Card(suit, rank.Rank, rank.Points))
            );

            Cards = new Stack<Card>(allCards);
        }
    }
}
