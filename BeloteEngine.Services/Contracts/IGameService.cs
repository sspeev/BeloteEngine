using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        void GameInitializer(Lobby lobby);
        void InitialPhase(Lobby lobby);
        Game Gameplay(Lobby lobby);
        Player PlayerToSplitCards(Lobby lobby);
        Player PlayerToDealCards(Lobby lobby);
        Player PlayerToStartAnnounce(Lobby lobby);
        bool IsGameOver(int team1Score, int team2Score);
        void DealCards(Lobby lobby, int count);
        void SetPlayerAnnounce(Player currPlayer, Announces announce);
        Player NextPlayerToAnnounce(Lobby lobby, Player currPlayer);
        Game GameReset(Lobby lobby);
        Game NextGame(Lobby lobby);
        Game Creator();
    }
}
