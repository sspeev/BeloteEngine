namespace BeloteEngine.Data.Entities.Models
{
    public class Deck
    {
        public Card[] Cards { get; set; }

        public Deck()
        {
            Cards =
            [
                new Card("Спатия", "7", 0),
                new Card("Спатия", "8", 0),
                new Card("Спатия", "9", 0),
                new Card("Спатия", "10", 10),
                new Card("Спатия", "J", 2),
                new Card("Спатия", "Q", 3),
                new Card("Спатия", "K", 4),
                new Card("Спатия", "A", 11),

                new Card("Каро", "7", 0),
                new Card("Каро", "8", 0),
                new Card("Каро", "9", 0),
                new Card("Каро", "10", 10),
                new Card("Каро", "J", 2),
                new Card("Каро", "Q", 3),
                new Card("Каро", "K", 4),
                new Card("Каро", "A", 11),

                new Card("Купа", "7", 0),
                new Card("Купа", "8", 0),
                new Card("Купа", "9", 0),
                new Card("Купа", "10", 10),
                new Card("Купа", "J", 2),
                new Card("Купа", "Q", 3),
                new Card("Купа", "K", 4),
                new Card("Купа", "A", 11),

                new Card("Пика", "7", 0),
                new Card("Пика", "8", 0),
                new Card("Пика", "9", 0),
                new Card("Пика", "10", 10),
                new Card("Пика", "J", 2),
                new Card("Пика", "Q", 3),
                new Card("Пика", "K", 4),
                new Card("Пика", "A", 11)
            ];
        }
    }
}
