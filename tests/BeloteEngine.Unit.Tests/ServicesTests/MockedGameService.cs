using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Models;

namespace BeloteEngine.Unit.Tests.Services;

internal sealed class MockedGameService : IGameService
{
    private readonly Func<Game> _gameFactory;

    public MockedGameService(Func<Game>? gameFactory = null)
    {
        _gameFactory = gameFactory ?? (() => new Game());
    }

    public void GameInitializer(Lobby lobby) => throw new NotSupportedException();

    public void InitialPhase(Lobby lobby) => throw new NotSupportedException();

    public Game Gameplay(Lobby lobby) => throw new NotSupportedException();

    public Player PlayerToSplitCards(Queue<Player> players) => throw new NotSupportedException();

    public Player PlayerToDealCards(Queue<Player> players) => throw new NotSupportedException();

    public Player PlayerToStartAnnounceAndPlay(Queue<Player> players) => throw new NotSupportedException();

    public Player GetNextBidder(Lobby lobby) => throw new NotSupportedException();

    public Player GetNextPlayer(Queue<Player> players) => throw new NotSupportedException();

    public bool IsGameOver(int team1Score, int team2Score) => throw new NotSupportedException();

    public void GetPlayerCards(Player player, Deck deck) => throw new NotSupportedException();

    public Player MakeBid(string playerName, string bid, Lobby lobby) => throw new NotSupportedException();

    public PlayCardResult PlayCard(string playerName, Card card, Lobby lobby) => throw new NotSupportedException();

    public Game GameReset(Lobby lobby) => throw new NotSupportedException();

    public Game Creator() => _gameFactory();
}
