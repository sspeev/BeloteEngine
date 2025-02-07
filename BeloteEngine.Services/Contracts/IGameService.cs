using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts
{
    public interface IGameService
    {
        void StartFirstPart();

        void SetPlayers();

        void StartSecondPart();

        Player PlayerToSplitCards(Team[] teams);

        bool IsGameOver(int team1Score, int team2Score);
    }
}
