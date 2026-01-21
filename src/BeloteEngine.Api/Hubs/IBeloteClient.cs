using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Api.Hubs;

//Server -> Client
public interface IBeloteClient
{
    Task PlayerJoined(int lobbyId, string playerName);

    Task PlayerLeft(int lobbyId, string playerName);

    Task LobbyUpdated(Lobby lobby);

    Task LobbyDeleted(int lobbyId);

    Task GameStarted(Lobby lobby);

    Task CardsDealt(Lobby lobby, string dealerName);

    Task BidMade(Lobby lobby);
}
