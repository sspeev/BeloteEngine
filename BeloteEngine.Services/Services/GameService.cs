using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.Extensions.Logging;
using static BeloteEngine.Data.Entities.Enums.Announces;

namespace BeloteEngine.Services.Services
{
    public class GameService(
        ILobby _lobby
        //,ILobbyService _lobbyService
        , ILogger<GameService> _logger)
        : IGameService
    {
        private readonly ILobby lobby = _lobby;
        //private readonly ILobbyService lobbyService = _lobbyService;
        private readonly ILogger<GameService> logger = _logger;

        public Game InitialPhase(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game), "Game cannot be null");
            }
            if (game.Players == null || game.Players.Length != 2 ||
                game.Players.Any(team => team.players == null || team.players.Length != 2))
            {
                throw new ArgumentException("Invalid teams array in the game");
            }
            if (game.Deck == null || game.Deck.Cards == null || game.Deck.Cards.Length == 0)
            {
                throw new InvalidOperationException("Deck is not initialized or has no cards");
            }

            game.Deck.Cards = CardsRandomizer(game.Deck.Cards);
            game.CurrentPlayer = PlayerToSplitCards(game.Players);
            logger.LogInformation("Current player to split cards: {PlayerName}", game.CurrentPlayer.Name);

            return game;

            // обявявания
            //IsAnnounceSet();
            //StartSecondPart();
        }

        public Game Gameplay(Game game)
        {
            throw new NotImplementedException();
        }



        public Player PlayerToSplitCards(Team[] teams)
        {
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
        public Player PlayerToDealCards(Team[] teams)
        {
            var splitter = lobby.Game.CurrentPlayer;
            var dealer = GetNextPlayer(teams, splitter);
            logger.LogInformation("Current player to deal cards: {PlayerName}", dealer.Name);
            return dealer;
        }

        public Player PlayerToStartAnnounce(Team[] teams)
        {
            var dealer = lobby.Game.CurrentPlayer;
            var announcer = GetNextPlayer(teams, dealer);
            announcer.IsStarter = true;
            logger.LogInformation("Current player to start announce: {PlayerName}", announcer.Name);
            return announcer;
        }

        private Player GetNextPlayer(Team[] teams, Player currentPlayer)
        {
            var players = AllPlayers(teams);
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

        public Game GameInitializer()
        {
            Game game = new()
            {
                Players = SetPlayers()
            };
            int teamRandomResult = new Random().Next(0, 2); // 0 or 1
            int playerRandomResult = new Random().Next(0, 2); // 0 or 1

            game.Players[teamRandomResult].players[playerRandomResult].LastSplitter = true;
            game.Deck = new Deck();
            lobby.GameStarted = true;

            logger.LogInformation("Game initialized with players: {Players}",
                string.Join(", ", game.Players.SelectMany(team => team.players.Select(player => player.Name))));
            return game;
        }

        private Team[] SetPlayers()
        {
            Team team1 = new()
            {
                players =
                [
                    lobby.ConnectedPlayers[0],
                    lobby.ConnectedPlayers[2]
                ],
                Score = 0
            };

            Team team2 = new()
            {
                players =
                [
                    lobby.ConnectedPlayers[1],
                    lobby.ConnectedPlayers[3]
                ],
                Score = 0
            };

            return [team1, team2];
        }

        private static Card[] CardsRandomizer(Card[] cards)
        {
            if (cards == null || cards.Length == 0)
            {
                throw new ArgumentException("Cards cannot be null or empty");
            }
            Random random = new();
            return [.. cards.OrderBy(x => random.Next())];
        }

        public void SetPlayerAnnounce(Player currPlayer, Announces announce)
        {
            currPlayer.AnnounceOffer = announce;
        }

        public Player NextPlayerToAnnounce(Player currPlayer)
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

                    return GetNextPlayer(lobby.Game.Players, currPlayer);
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

        public Game GameReset(Game game)
        {
            game.CurrentAnnounce = None;
            game.PassCounter = 0;

            return InitialPhase(game);
        }

        public Game NextGame(Game game)
        {
            //Calculate points

            game.CurrentAnnounce = None;
            game.PassCounter = 0;

            return InitialPhase(game);
        }
    }
}
