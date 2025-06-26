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
                    case Announces.Спатия:
                        if (card.Suit == "Спатия")
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Каро:
                        if (card.Suit == "Каро")
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Купа:
                        if (card.Suit == "Купа")
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Пика:
                        if (card.Suit == "Пика")
                        {
                            if (card.Rank == "9") card.Points = 14;
                            if (card.Rank == "J") card.Points = 20;
                        }
                        break;
                    case Announces.Без_Коз:
                        if (card.Rank == "9") card.Points = 0;
                        if (card.Rank == "J") card.Points = 2;
                        break;
                    case Announces.Всичко_Коз:
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
