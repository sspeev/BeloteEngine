using BeloteEngine.Data.Entities.Enums;

namespace BeloteEngine.Data.Entities.Models
{
    public class Game
    {
        public Game()
        {
            Players = new Team[2];
            SetCards();
        }

        public Announces CurrentAnnounce { get; set; } = 0;

        public Team[] Players { get; set; }

        public static Dictionary<string, Dictionary<string, int>> Cards { get; set; }

        private void SetCards() => Cards = new Dictionary<string, Dictionary<string, int>>()
            {
                {
                    "Спатия", new Dictionary<string, int>()
                    {
                        { "7", 0},
                        {"8", 0},
                        {"9", 0},
                        { "10", 10},
                        { "J", 2},
                        { "Q", 3},
                        { "K", 4},
                        {"A", 11 }
                    }
                },
                {
                    "Каро", new Dictionary<string, int>()
                    {
                        { "7", 0},
                        {"8", 0},
                        {"9", 0},
                        { "10", 10},
                        { "J", 2},
                        { "Q", 3},
                        { "K", 4},
                        {"A", 11 }
                    }
                },
                {
                    "Купа", new Dictionary<string, int>()
                    {
                        { "7", 0},
                        {"8", 0},
                        {"9", 0},
                        { "10", 10},
                        { "J", 2},
                        { "Q", 3},
                        { "K", 4},
                        {"A", 11 }
                    }
                },
                {
                    "Пика", new Dictionary<string, int>()
                    {
                        { "7", 0},
                        {"8", 0},
                        {"9", 0},
                        { "10", 10},
                        { "J", 2},
                        { "Q", 3},
                        { "K", 4},
                        {"A", 11 }
                    }
                },
            };

        public void SetPointsOnCards()
        {
            switch (CurrentAnnounce)
            {
                case Announces.Спатия:
                    Cards["Спатия"]["9"] = 14;
                    Cards["Спатия"]["J"] = 20;
                    break;
                case Announces.Каро:
                    Cards["Каро"]["9"] = 14;
                    Cards["Каро"]["J"] = 20;
                    break;
                case Announces.Купа:
                    Cards["Купа"]["9"] = 14;
                    Cards["Купа"]["J"] = 20;
                    break;
                case Announces.Пика:
                    Cards["Пика"]["9"] = 14;
                    Cards["Пика"]["J"] = 20;
                    break;
                case Announces.Без_Коз:
                    Cards["Спатия"]["9"] = 0;
                    Cards["Спатия"]["J"] = 2;
                    Cards["Каро"]["9"] = 0;
                    Cards["Каро"]["J"] = 2;
                    Cards["Купа"]["9"] = 0;
                    Cards["Купа"]["J"] = 2;
                    Cards["Пика"]["9"] = 0;
                    Cards["Пика"]["J"] = 2;
                    break;
                case Announces.Всичко_Коз:
                    Cards["Спатия"]["9"] = 14;
                    Cards["Спатия"]["J"] = 20;
                    Cards["Каро"]["9"] = 14;
                    Cards["Каро"]["J"] = 20;
                    Cards["Купа"]["9"] = 14;
                    Cards["Купа"]["J"] = 20;
                    Cards["Пика"]["9"] = 14;
                    Cards["Пика"]["J"] = 20;
                    break;
                default:
                    break;
            }
        }
    }
}
