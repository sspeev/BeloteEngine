using BeloteEngine.Application.DTOs;
using BeloteEngine.Domain.Entities.Models;
using LobbyModel = BeloteEngine.Domain.Entities.Models.Lobby;
using GameModel = BeloteEngine.Domain.Entities.Models.Game;

namespace BeloteEngine.Application.Contracts;

public interface IGameService
{
    public void GameInitializer(LobbyModel lobby);
    public void InitialPhase(LobbyModel lobby);
    public GameModel Gameplay(LobbyModel lobby);
    public Player PlayerToSplitCards(List<Player> players);
    public Player PlayerToDealCards(List<Player> players);
    public Player PlayerToStartAnnounceAndPlay(List<Player> players);
    public Player GetNextBidder(LobbyModel lobby);
    public Player GetNextPlayer(List<Player> players);
    public bool IsGameOver(int team1Score, int team2Score);
    void GetPlayerCards(Player player, Deck deck);
    Player MakeBid(string playerName, string bid, LobbyModel lobby);
    PlayCardResult PlayCard(string playerName, Card card, LobbyModel lobby);
    GameModel GameReset(LobbyModel lobby);
    GameModel Creator();
}
