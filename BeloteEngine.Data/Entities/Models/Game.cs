using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Game
    {
        public Team[] Players { get; init; } = new Team[2];

        public Deck Deck { get; set; } = new();

        public void SetPointsOnCards()
        {
            foreach (var card in Deck.Cards)
            {
                switch (CurrentAnnounce)
                {
                    case Announces.Clubs:
                        if (card.Suit == Suit.Clubs)
                        {
                            card.Points = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Points
                            };
                        }
                        break;
                    case Announces.Diamonds:
                        if (card.Suit == Suit.Diamonds)
                        {
                            card.Points = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Points
                            };
                        }
                        break;
                    case Announces.Hearts:
                        if (card.Suit == Suit.Hearts)
                        {
                            card.Points = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Points
                            };
                        }
                        break;
                    case Announces.Spades:
                        if (card.Suit == Suit.Spades)
                        {
                            card.Points = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Points
                            };
                        }
                        break;
                    case Announces.Without_Announce:
                        card.Points = card.Rank switch
                        {
                            "9" => 0,
                            "J" => 2,
                            _ => card.Points
                        };
                        break;
                    case Announces.All_Announce:
                        card.Points = card.Rank switch
                        {
                            "9" => 14,
                            "J" => 20,
                            _ => card.Points
                        };
                        break;
                    case Announces.None:
                    case Announces.Pass:
                    default:
                        break;
                }
            }
        }


        public Announces CurrentAnnounce { get; set; } = Announces.None;

        public Player CurrentPlayer { get; set; } = null!;

        public int PassCounter { get; set; }
    }
}
