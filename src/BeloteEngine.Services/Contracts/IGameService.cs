using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Contracts;

public interface IGameService
{
    public void GameInitializer(Lobby lobby);
    public void InitialPhase(Lobby lobby);
    public Game Gameplay(Lobby lobby);
    public Player PlayerToSplitCards(Queue<Player> players);
    public Player PlayerToDealCards(Queue<Player> players);
    public Player PlayerToStartAnnounceAndPlay(Queue<Player> players);
    public Player GetNextPlayer(Queue<Player> players);
    public bool IsGameOver(int team1Score, int team2Score);
    void GetPlayerCards(string playerName, Lobby lobby);
    void MakeBid(string playerName, string bid, Lobby lobby);
    Game GameReset(Lobby lobby);
    Game NextGame(Lobby lobby);
    Game Creator();
}
