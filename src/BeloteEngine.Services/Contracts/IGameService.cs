using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Models;

namespace BeloteEngine.Services.Contracts;

public interface IGameService
{
    public void GameInitializer(Lobby lobby);
    public void InitialPhase(Lobby lobby);
    public Game Gameplay(Lobby lobby);
    public Player PlayerToSplitCards(Queue<Player> players);
    public Player PlayerToDealCards(Queue<Player> players);
    public Player PlayerToStartAnnounceAndPlay(Queue<Player> players);
    public Player GetNextBidder(Lobby lobby);
    public Player GetNextPlayer(Queue<Player> players);
    public bool IsGameOver(int team1Score, int team2Score);
    void GetPlayerCards(Player player, Deck deck);
    Player MakeBid(string playerName, string bid, Lobby lobby);
    PlayCardResult PlayCard(string playerName, Card card, Lobby lobby);
    Game GameReset(Lobby lobby);
    Game NextGame(Lobby lobby);
    Game Creator();
}
