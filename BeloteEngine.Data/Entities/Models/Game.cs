using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Game
    {
        public Game()
        {
            Players = new Team[2];
            Deck = new Deck();
        }

        public Team[] Players { get; set; }

        public Deck Deck { get; set; }

        public void SetPointsOnCards()
        {
            foreach (var card in Deck.Cards)
            {
                switch (CurrentAnnounce)
                {
                    case Announces.Clubs:
                        if (card.Suit == Suit.Clubs)
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Diamonds:
                        if (card.Suit == Suit.Diamonds)
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Hearths:
                        if (card.Suit == Suit.Hearts)
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Spades:
                        if (card.Suit == Suit.Spades)
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Without_Announce:
                        if (card.Rank == "9") card.Points = 0;
                        if (card.Rank == "J") card.Points = 2;
                        break;
                    case Announces.All_Announce:
                        if (card.Rank == "9") card.Points = 14;
                        if (card.Rank == "J") card.Points = 20;
                        break;
                    default:
                        break;
                }
            }
        }


        public Announces CurrentAnnounce { get; set; } = Announces.None;

        public Player CurrentPlayer { get; set; } = null!;

        public int PassCounter { get; set; } = 0;
    }
}
