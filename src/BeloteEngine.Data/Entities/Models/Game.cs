using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Data.Entities.Models
{
    public class Game
    {
        public Team[] Teams { get; init; } = new Team[2];

        public Queue<Player> SortedPlayers { get; set; } = new();
        public Queue<Player> RoundQueue { get; set; } = new(); // Tracks round rotation
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
                            card.Value = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Value
                            };
                        }
                        break;
                    case Announces.Diamonds:
                        if (card.Suit == Suit.Diamonds)
                        {
                            card.Value = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Value
                            };
                        }
                        break;
                    case Announces.Hearts:
                        if (card.Suit == Suit.Hearts)
                        {
                            card.Value = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Value
                            };
                            card.Power = card.Rank switch
                            {
                                "J" => 8,
                                "9" => 7,
                                "A" => 6,
                                "10" => 5,
                                "K" => 4,
                                "Q" => 3,
                                "8" => 2,
                                "7" => 1,
                                _ => card.Power
                            };
                        }
                        break;
                    case Announces.Spades:
                        if (card.Suit == Suit.Spades)
                        {
                            card.Value = card.Rank switch
                            {
                                "9" => 14,
                                "J" => 20,
                                _ => card.Value
                            };
                            card.Power = card.Rank switch
                            {
                                "J" => 8,
                                "9" => 7,
                                "A" => 6,
                                "10" => 5,
                                "K" => 4,
                                "Q" => 3,
                                "8" => 2,
                                "7" => 1,
                                _ => card.Power
                            };
                        }
                        break;
                    case Announces.NoTrump:
                        card.Value = card.Rank switch
                        {
                            "9" => 0,
                            "J" => 2,
                            _ => card.Value
                        };
                        card.Power = card.Rank switch
                        {
                            "A" => 8,
                            "10" => 7,
                            "K" => 6,
                            "Q" => 5,
                            "J" => 4,
                            "9" => 3,
                            "8" => 2,
                            "7" => 1,
                            _ => card.Power
                        };
                        break;
                    case Announces.AllTrumps:
                        card.Value = card.Rank switch
                        {
                            "9" => 14,
                            "J" => 20,
                            _ => card.Value
                        };
                        card.Power = card.Rank switch
                        {
                            "J" => 8,
                            "9" => 7,
                            "A" => 6,
                            "10" => 5,
                            "K" => 4,
                            "Q" => 3,
                            "8" => 2,
                            "7" => 1,
                            _ => card.Power
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

        public Player Splitter { get; set; } = null!;

        public Player Dealer { get; set; } = null!;

        public Player Announcer { get; set; } = null!;

        public Player Starter { get; set; } = null!;

        public Round? CurrentRound { get; set; }    

        public int PassCounter { get; set; }
    }
}
