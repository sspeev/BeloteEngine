using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;

namespace BeloteEngine.Services.Services
{
    public class GameService : IGameService
    {
        private bool IsAnnounceSet(Team[] teams)
        {
            throw new NotImplementedException();
        }

        public void SetPlayers()
        {
            Team[] teams = new Team[2];
            for (int i = 0; i < 2; i++)
            {
                teams[i].players = new Player[2];
                //for (int j = 0; j < 2; j++)
                //{
                //    teams[i].players[j];
                //}
            }
        }

        public void StartFirstPart()
        {
            //Player 1 цепи
            //Player 2 раздава
            //Player 1 под ръка
            // обявявания
            //IsAnnounceSet();
            //StartSecondPart();
        }

        public void StartSecondPart()
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, Dictionary<string, int>> CardsRandomizer(Dictionary<string, Dictionary<string, int>> cards)
        {
            throw new NotImplementedException();
        }
    }
}
