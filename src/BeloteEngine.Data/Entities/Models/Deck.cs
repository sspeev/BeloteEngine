using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Deck
    {
        public Stack<Card> Cards { get; set; }

        public Deck()
        {
            var suits = new List<Suit> { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };
            var cards = new List<(string rank, int value, int power)>
            {
                ("7", 0, 1),
                ("8", 0, 2),
                ("9", 0, 3),
                ("10", 10, 4),
                ("J", 2, 5),
                ("Q", 3, 6),
                ("K", 4, 7),
                ("A", 11, 8)
            };

            var allCards = suits.SelectMany(suit =>
                cards.Select(card => new Card(suit, card.rank, card.value, card.power))
            );

            Cards = new Stack<Card>(allCards);
        }
    }
}
