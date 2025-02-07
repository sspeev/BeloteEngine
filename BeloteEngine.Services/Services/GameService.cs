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

        public Player PlayerToSplitCards(Team[] teams)
        {
            if (teams == null || teams.Length != 2 || teams.Any(team => team.players == null || team.players.Length != 2))
            {
                throw new ArgumentException("Invalid teams array");
            }

            bool isGameStarted = teams.Any(team => team.Score != 0);

            if (isGameStarted)
            {
                for (int i = 0; i < teams.Length; i++)
                {
                    for (int j = 0; j < teams[i].players.Length; j++)
                    {
                        if (teams[i].players[j].LastSplitter)
                        {
                            teams[i].players[j].LastSplitter = false;
                            teams[(i + 1) % 2].players[j].LastSplitter = true;
                            return teams[(i + 1) % 2].players[j];
                        }
                    }
                }
            }

            var randomizer = new Random();
            const int totalPlayers = 4;
            const int playersPerTeam = 2;

            int indexOfPlayer = randomizer.Next(0, totalPlayers); // 0 to 3 inclusive
            int indexOfTeam = indexOfPlayer / playersPerTeam; // 0 or 1
            indexOfPlayer %= playersPerTeam; // 0 or 1

            teams[indexOfTeam].players[indexOfPlayer].LastSplitter = true;
            return teams[indexOfTeam].players[indexOfPlayer];
        }

        public bool IsGameOver(int team1Score, int team2Score)
        {
            throw new NotImplementedException();
        }
    }
}
