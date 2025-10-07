using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.Extensions.Logging;
using static BeloteEngine.Data.Entities.Enums.Announces;

namespace BeloteEngine.Services.Services
{
    public class GameService(
        //ILobbyService _lobbyService
         ILogger<GameService> _logger)
        : IGameService
    {
        //private readonly ILobbyService lobbyService = _lobbyService;
        private readonly ILogger<GameService> logger = _logger;

        public void InitialPhase(Lobby lobby)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby), "Lobby cannot be null");
            }
            if (lobby.Game == null)
            {
                throw new InvalidOperationException("Game is not initialized in the lobby");
            }
            var game = lobby.Game;
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game), "Game cannot be null");
            }
            if (game.Players == null || game.Players.Length != 4 ||
                game.Players.Any(team => team.players == null || team.players.Length != 2))
            {
                throw new ArgumentException("Invalid teams array in the game");
            }

            game.Deck.Cards = CardsRandomizer(game.Deck.Cards);
            game.CurrentPlayer = PlayerToSplitCards(lobby);
            logger.LogInformation("Current player to split cards: {PlayerName}", game.CurrentPlayer.Name);

            DealCards(lobby, 3);
            DealCards(lobby, 2);

            // обявявания
            //IsAnnounceSet();
            //StartSecondPart();
        }

        public Game Gameplay(Lobby lobby)
        {
            throw new NotImplementedException();
        }

        public Player PlayerToSplitCards(Lobby lobby)
        {
            var teams = lobby.Game.Players;
            if (teams == null || teams.Length != 2 || teams.Any(team => team.players == null || team.players.Length != 2))
            {
                throw new ArgumentException("Invalid teams array");
            }

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
            throw new InvalidOperationException("No player found who has split cards last.");
        }
        public Player PlayerToDealCards(Lobby lobby)
        {
            var splitter = lobby.Game.CurrentPlayer;
            var dealer = GetNextPlayer(lobby, splitter);
            logger.LogInformation("Current player to deal cards: {PlayerName}", dealer.Name);
            return dealer;
        }

        public Player PlayerToStartAnnounce(Lobby lobby)
        {
            var dealer = lobby.Game.CurrentPlayer;
            var announcer = GetNextPlayer(lobby, dealer);
            announcer.IsStarter = true;
            logger.LogInformation("Current player to start announce: {PlayerName}", announcer.Name);
            return announcer;
        }

        private Player GetNextPlayer(Lobby lobby, Player currentPlayer)
        {
            var players = AllPlayers(lobby.Game.Players);
            int playerIndex = Array.IndexOf(players, currentPlayer);
            var nextPlayer = players[(playerIndex + 1) % players.Length];
            lobby.Game.CurrentPlayer = nextPlayer;
            logger.LogInformation("Next player to make an action: {PlayerName}", nextPlayer.Name);
            return nextPlayer;
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

        public void GameInitializer(Lobby lobby)
        {
            Game game = new()
            {
                Players = SetPlayers(lobby.ConnectedPlayers)
            };
            int teamRandomResult = new Random().Next(0, 2); // 0 or 1
            int playerRandomResult = new Random().Next(0, 2); // 0 or 1

            game.Players[teamRandomResult].players[playerRandomResult].LastSplitter = true;
            game.Deck = new Deck();
            
            // Assign the game to the lobby
            lobby.Game = game;
            lobby.GameStarted = true;

            logger.LogInformation("Game initialized with players: {Players}",
                string.Join(", ", game.Players.SelectMany(team => team.players.Select(player => player.Name))));
        }

        private static Team[] SetPlayers(List<Player> connectedPlayers)
        {
            Team team1 = new()
            {
                players =
                [
                    connectedPlayers[0],
                    connectedPlayers[2]
                ],
                Score = 0
            };

            Team team2 = new()
            {
                players =
                [
                    connectedPlayers[1],
                    connectedPlayers[3]
                ],
                Score = 0
            };

            return [team1, team2];
        }

        private static Stack<Card> CardsRandomizer(Stack<Card> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                throw new ArgumentException("Cards cannot be null or empty");
            }
            Random random = new();
            var shuffledList = cards.OrderBy(x => random.Next()).ToList();
            return new Stack<Card>(shuffledList);
        }

        public void DealCards(Lobby lobby, int count)
        {
            var players = AllPlayers(lobby.Game.Players);
            var deck = lobby.Game.Deck.Cards;

            foreach (var player in players)
            {
                for (int i = 0; i < count; i++)
                {
                    if (deck.TryPop(out var card))
                    {
                        player.Hand.Add(card);
                    }
                }
            }
        }

        public void SetPlayerAnnounce(Player currPlayer, Announces announce)
        {
            currPlayer.AnnounceOffer = announce;
        }

        public Player NextPlayerToAnnounce(Lobby lobby, Player currPlayer)
        {
            if (currPlayer.AnnounceOffer == None)
            {
                throw new ArgumentNullException("Current player has not announced yet!");
            }
            if(currPlayer.AnnounceOffer != Пас)
            {
                if(lobby.Game.CurrentAnnounce < currPlayer.AnnounceOffer)
                {
                    logger.LogInformation("Current announce updated to: {Announce}", currPlayer.AnnounceOffer);
                    lobby.Game.CurrentAnnounce = currPlayer.AnnounceOffer;

                    return GetNextPlayer(lobby, currPlayer);
                }
                else throw new InvalidOperationException("Current announce cannot be lower than the previous one!");
            }
            else lobby.Game.PassCounter++;

            throw new Exception("No next player to announce found or game is in an invalid state.");
        }

        //private Game EndStateOfInitialPhase()
        //{
        //    if (lobby.Game.PassCounter == 4)
        //    {
        //        return GameReset(lobby.Game);
        //    }
        //    else if (lobby.Game.PassCounter == 3 && lobby.Game.CurrentAnnounce != None)
        //    {
        //        Gameplay(lobby.Game);
        //    }
        //    else
        //    {

        //    }
        //}

        public Game GameReset(Lobby lobby)
        {
            var game = lobby.Game;
            game.CurrentAnnounce = None;
            game.PassCounter = 0;

            InitialPhase(lobby);
            return game;
        }

        public Game NextGame(Lobby lobby)
        {
            //Calculate points
            var game = lobby.Game;
            game.CurrentAnnounce = None;
            game.PassCounter = 0;

            InitialPhase(lobby);
            return game;
        }

        public Game Creator() => new();
    }
}
