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

        // Initialize both queues
        lobby.Game.RoundQueue = InitSortedPlayers(lobby.Game.Teams);
        lobby.Game.SortedPlayers = new Queue<Player>(lobby.Game.RoundQueue); // Copy for gameplay

        lobby.Game.CurrentPlayer = PlayerToSplitCards(lobby.Game.SortedPlayers);
        lobby.GamePhase = "splitting";
        logger.LogInformation("Current player to split cards: {PlayerName}", lobby.Game.CurrentPlayer.Name);
    }

    public Game Gameplay(Lobby lobby)
    {
        throw new NotImplementedException("Gameplay logic is not implemented yet.");
    }

    public Player PlayerToSplitCards(Queue<Player> players)
    {
        var splitter = RotatePlayerQueue(players);
        logger.LogInformation("Current player to split cards: {PlayerName}", splitter.Name);
        return splitter;
    }
    public Player PlayerToDealCards(Queue<Player> players)
    {
        var dealer = RotatePlayerQueue(players);
        logger.LogInformation("Current player to deal cards: {PlayerName}", dealer.Name);
        return dealer;
    }
    public Player PlayerToStartAnnounceAndPlay(Queue<Player> players)
    {
        var announcer = RotatePlayerQueue(players);
        logger.LogInformation("Current player to start announce: {PlayerName}", announcer.Name);
        return announcer;
    }
    public Player GetNextPlayer(Queue<Player> players)
    {
        var nextPlayer = RotatePlayerQueue(players);
        logger.LogInformation("Next player to make an action: {PlayerName}", nextPlayer.Name);
        return nextPlayer;
    }
    private static Player RotatePlayerQueue(Queue<Player> players)
    {
        var player = players.Dequeue();
        players.Enqueue(player);
        return player;
    }

    public Player GetNextBidder(Lobby lobby)
    {
        var tempPlayers = lobby.Game.SortedPlayers;
        var nextPlayer = RotatePlayerQueue(tempPlayers); // Move to the next player after the current bidder
        logger.LogInformation("Next player to bid {PlayerName}", nextPlayer.Name);
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

    public void GetPlayerCards(Player player, Deck deck)
    {
        for (int i = 1; i <= 8; i++)
        {
            if (deck.Cards.TryPop(out var card))
            {
                player.Hand.Add(card);
            }
        }
    }

    public Player MakeBid(string playerName, string bid, Lobby lobby)
    {
        var player = lobby.ConnectedPlayers.FirstOrDefault(p => p.Name == playerName)
            ?? throw new ArgumentException($"Player {playerName} not found in the lobby.");

        if (!Enum.TryParse(bid, out Announces announce))
        {
            throw new ArgumentException($"Invalid bid: {bid} or failed to parse");
        }
        player.AnnounceOffer = announce;

        // Check if the player is passing or making a real bid
        if (announce != Pass)
        {
            // Player made a real bid - check if it's higher than current announce
            if (lobby.Game.CurrentAnnounce != None && lobby.Game.CurrentAnnounce < announce)
            {
                logger.LogInformation("Current announce updated to: {Announce}", announce);
                lobby.Game.CurrentAnnounce = announce;
                lobby.Game.PassCounter = 0;
            }
            else if (lobby.Game.CurrentAnnounce == None)
            {
                // First real bid
                logger.LogInformation("First announce set to: {Announce}", announce);
                lobby.Game.CurrentAnnounce = announce;
            }
            else
            {
                throw new InvalidOperationException("Your bid must be higher than the current announce!");
            }
        }
        else
        {
            // Player passed
            lobby.Game.PassCounter++;
            logger.LogInformation("Player {PlayerName} passed. Pass counter: {PassCounter}",
                playerName, lobby.Game.PassCounter);
        }

        logger.LogInformation("Player {PlayerName} made a bid: {Bid}", playerName, bid);

        var nextPlayer = GetNextBidder(lobby);
        lobby.Game.CurrentPlayer = nextPlayer;

        return nextPlayer;
    }

    public Game GameReset(Lobby lobby)
    {
        ValidateLobby(lobby);

        var game = lobby.Game;

        // Reset bidding state
        game.CurrentAnnounce = None;
        game.PassCounter = 0;

        // Clear player hands for new deal
        foreach (var player in lobby.ConnectedPlayers)
        {
            player.Hand.Clear();
            player.AnnounceOffer = None;
        }

        // Create a new deck for the new round
        game.Deck = new Deck();
        game.Deck.Cards = CardsRandomizer(game.Deck.Cards);

        // Advance the RoundQueue clockwise for next round
        // This rotates: P1 → P2 → P3 → P4 → P1
        RotatePlayerQueue(game.RoundQueue);

        // Rebuild SortedPlayers from RoundQueue for the new round
        game.SortedPlayers = new Queue<Player>(game.RoundQueue);

        // Set the current player to the splitter (first in queue)
        game.CurrentPlayer = PlayerToSplitCards(game.SortedPlayers);
        lobby.GamePhase = "splitting";

        logger.LogInformation("Game reset in lobby. New splitter: {PlayerName}", game.CurrentPlayer.Name);

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
