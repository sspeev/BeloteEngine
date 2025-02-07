using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        void SetPlayers();
        void StartFirstPart();

        void StartSecondPart();

        public Player PlayerToSplitCards(Team[] teams);

        //public Player PlayerToDealCards(Team[] teams);
        public Player PlayerToStartAnnounce(Team[] teams);

        //public Player NextPlayerToAnnounce(Player player);

        bool IsGameOver(int team1Score, int team2Score);
    }
}
