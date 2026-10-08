using BeloteEngine.Domain.Entities.Enums;
using BeloteEngine.Domain.Entities.Models;
using LobbyModel = BeloteEngine.Domain.Entities.Models.Lobby;
using GameModel = BeloteEngine.Domain.Entities.Models.Game;

namespace BeloteEngine.Application.Contracts.Game;

public interface IGameValidation
{
    void ValidateLobby(LobbyModel lobby);
    Round ValidateActiveRound(GameModel game);
    Player ValidatePlayer(LobbyModel lobby, string playerName);
    Player ValidatePlayerForCardPlay(LobbyModel lobby, string playerName);
    void ValidateTurn(GameModel game, string playerName);
    void ValidateCardPlay(Card card, Player player, Trick trick, Announces trump);
    Announces ParseBid(string bid);
    void ValidateBid(GameModel game, Player player, Announces announce);
}

