using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        public Game GameInitializer();

        public Game InitialPhase(Game game);

        public Game Gameplay(Game game);

        public Player PlayerToSplitCards(Team[] teams);

        public Player PlayerToDealCards(Team[] teams);

        public Player PlayerToStartAnnounce(Team[] teams);

        public void SetPlayerAnnounce(Player player, Announces announce);

        public Player NextPlayerToAnnounce(Player player);

        bool IsGameOver(int team1Score, int team2Score);

        //public Team Winner(Team[] teams);

        public Game NextGame(Game game);

        public Game GameReset(Game game);
    }
}
