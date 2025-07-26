using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class.
        /// </summary>
        /// <returns>A <see cref="Game"/> object representing the initialized game state.</returns>
        public Game GameInitializer(Lobby lobby);

        public Game Creator();

        public Game InitialPhase(Lobby lobby);

        public Game Gameplay(Lobby lobby);

        public Player PlayerToSplitCards(Lobby lobby);

        public Player PlayerToDealCards(Lobby lobby);

        public Player PlayerToStartAnnounce(Lobby lobby);

        public void SetPlayerAnnounce(Player player, Announces announce);

        public Player NextPlayerToAnnounce(Lobby lobby, Player currPlayer);

        bool IsGameOver(int team1Score, int team2Score);

        //public Team Winner(Team[] teams);

        public Game NextGame(Lobby lobby);

        public Game GameReset(Lobby lobby);
    }
}
