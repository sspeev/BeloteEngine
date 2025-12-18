using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        public void GameInitializer(Lobby lobby);
        public void InitialPhase(Lobby lobby);
        public Game Gameplay(Lobby lobby);
        public Player PlayerToSplitCards(Queue<Player> players);
        public Player PlayerToDealCards(Queue<Player> players);
        public Player PlayerToStartAnnounceAndPlay(Queue<Player> players);
        public Player GetNextPlayer(Queue<Player> players);
        public bool IsGameOver(int team1Score, int team2Score);
        //public void DealCards(Lobby lobby, int count);
        public Player NextPlayerToAnnounce(Game game);
        Game GameReset(Lobby lobby);
        Game NextGame(Lobby lobby);
        Game Creator();
    }
}
