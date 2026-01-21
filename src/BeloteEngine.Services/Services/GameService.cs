using BeloteEngine.Data.Entities.Enums;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using Microsoft.Extensions.Logging;
using static BeloteEngine.Data.Entities.Enums.Announces;

namespace BeloteEngine.Services.Services;

public class GameService(
      ILogger<GameService> logger)
    : IGameService
{
    private static void ValidateLobby(Lobby lobby)
    {
        ArgumentNullException.ThrowIfNull(lobby);
        ArgumentNullException.ThrowIfNull(lobby.Game);

        if (lobby.Game.Teams == null || lobby.Game.Teams.Any(t => t.Players.Length != 2))
        {
            throw new ArgumentException($"Invalid teams configuration. Each team must have exactly 2 players.");
        }
    }

    public void InitialPhase(Lobby lobby)
    {
        ValidateLobby(lobby);

        lobby.Game.Deck.Cards = CardsRandomizer(lobby.Game.Deck.Cards);
        lobby.Game.SortedPlayers = InitSortedPlayers(lobby.Game.Teams);
        lobby.Game.CurrentPlayer = PlayerToSplitCards(lobby.Game.SortedPlayers);
        lobby.GamePhase = "splitting";
        logger.LogInformation("Current player to split cards: {PlayerName}", lobby.Game.CurrentPlayer.Name);
    }

    public Game Gameplay(Lobby lobby)
    {
        throw new NotImplementedException();
    }

    public Player PlayerToSplitCards(Queue<Player> players)
    {
        var splitter = players.Peek();
        splitter.Splitter = true;
        logger.LogInformation("Current player to split cards: {PlayerName}", splitter.Name);
        return splitter;
    }

    public Player PlayerToDealCards(Queue<Player> players)
    {
        var splitter = players.Dequeue();
        splitter.Splitter = false;
        players.Enqueue(splitter);

        var dealer = players.Peek();
        dealer.Dealer = true;
        logger.LogInformation("Current player to deal cards: {PlayerName}", dealer.Name);
        return dealer;
    }

    public Player PlayerToStartAnnounceAndPlay(Queue<Player> players)
    {
        var dealer = players.Dequeue();
        dealer.Dealer = false;
        players.Enqueue(dealer);

        var announcer = players.Peek();
        players.Enqueue(announcer);
        logger.LogInformation("Current player to start announce: {PlayerName}", announcer.Name);
        return announcer;
    }

    public Player GetNextPlayer(Queue<Player> players)
    {
        var nextPlayer = players.Dequeue();
        players.Enqueue(nextPlayer);
        logger.LogInformation("Next player to make an action: {PlayerName}", nextPlayer.Name);
        return nextPlayer;
    }

    public bool IsGameOver(int team1Score, int team2Score)
    {
        return team1Score >= 151 || team2Score >= 151;
    }

    private static Queue<Player> InitSortedPlayers(Team[] teams)
    {
        Queue<Player> sortedPlayers = new();
        sortedPlayers.Enqueue(teams[0].Players[0]);
        sortedPlayers.Enqueue(teams[1].Players[0]);
        sortedPlayers.Enqueue(teams[0].Players[1]);
        sortedPlayers.Enqueue(teams[1].Players[1]);
        return sortedPlayers;
    }

    public void GameInitializer(Lobby lobby)
    {
        Game game = new()
        {
            Teams = SetPlayers(lobby.ConnectedPlayers)
        };
        game.Deck = new Deck();

        // Assign the game to the lobby
        lobby.Game = game;
        lobby.GameStarted = true;

        logger.LogInformation("Game initialized with players: {Players}",
            string.Join(", ", game.Teams.SelectMany(team => team.Players.Select(player => player.Name))));
    }

    private static Team[] SetPlayers(List<Player> connectedPlayers)
    {
        Team team1 = new()
        {
            Players =
            [
                connectedPlayers[0],
                connectedPlayers[2]
            ],
            Score = 0
        };

        Team team2 = new()
        {
            Players =
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
        var shuffledList = cards.OrderBy(_ => random.Next()).ToList();
        var firstHalf = shuffledList.Take(16).ToList();
        var secondHalf = shuffledList.Skip(16).Take(16).ToList();

        return new Stack<Card>(firstHalf.Concat(secondHalf));
    }

    public void GetPlayerCards(string playerNamem, Lobby lobby)
    {
        var deck = lobby.Game.Deck.Cards;

        foreach (var player in lobby.ConnectedPlayers)
        {
            for (int i = 0; i < 4; i++)
            {
                if (deck.TryPop(out var card))
                {
                    player.Hand.Add(card);
                }
            }
        }
    }

    public void MakeBid(string playerName, string bid, Lobby lobby)
    {
        var player = lobby.ConnectedPlayers.FirstOrDefault(p => p.Name == playerName)
            ?? throw new ArgumentException($"Player {playerName} not found in the lobby.");

        if (!Enum.TryParse(bid, out Announces announce))
        {
            throw new ArgumentException($"Invalid bid: {bid} or failed to parse");
        }
        player.AnnounceOffer = announce;

        if (lobby.Game.CurrentAnnounce != Pass)
        {
            if (lobby.Game.CurrentAnnounce < player.AnnounceOffer)
            {
                logger.LogInformation("Current announce updated to: {Announce}", player.AnnounceOffer);
                lobby.Game.CurrentAnnounce = player.AnnounceOffer;

            }
            else throw new InvalidOperationException("Current announce cannot be lower than the previous one!");
        }
        else lobby.Game.PassCounter++;
        logger.LogInformation("Player {PlayerName} made a bid: {Bid}", playerName, bid);

        if (lobby.Game.PassCounter == 4)
        {
            logger.LogInformation("All players passed. Resetting the game.");
            lobby.Game = GameReset(lobby);
        }
        else Gameplay(lobby);
    }

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
