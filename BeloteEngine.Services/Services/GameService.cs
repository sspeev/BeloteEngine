using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;

namespace BeloteEngine.Services.Services
{
    public class GameService : IGameService
    {

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
        public Player PlayerToDealCards(Team[] teams)
        {
            var splitter = PlayerToSplitCards(teams);
            return GetNextPlayer(teams, splitter);
        }

        public Player PlayerToStartAnnounce(Team[] teams)
        {
            var dealer = PlayerToDealCards(teams);
            return GetNextPlayer(teams, dealer);
        }

        private static Player GetNextPlayer(Team[] teams, Player currentPlayer)
        {
            var players = AllPlayers(teams);
            int playerIndex = Array.IndexOf(players, currentPlayer);
            return players[(playerIndex + 1) % players.Length];
        }

        public bool IsGameOver(int team1Score, int team2Score)
        {
            if (team1Score >= 151 || team2Score >= 151)
            {
                return true;
            }
            return false;
        }

        private static Player[] AllPlayers(Team[] teams) => [
                teams[0].players[0],
                teams[1].players[0],
                teams[0].players[1],
                teams[1].players[1]
            ];
    }
}
