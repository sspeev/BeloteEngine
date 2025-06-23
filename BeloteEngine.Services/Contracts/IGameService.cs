using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        public Game GameInitializer();

        Team[] SetPlayers();

        public void StartFirstPart(Game game);

        void StartSecondPart();

        public Player PlayerToSplitCards(Team[] teams);

        public Player PlayerToDealCards(Team[] teams);

        public Player PlayerToStartAnnounce(Team[] teams);

        //public Player NextPlayerToAnnounce(Player player);

        bool IsGameOver(int team1Score, int team2Score);

        //public Team Winner(Team[] teams);
    }
}
