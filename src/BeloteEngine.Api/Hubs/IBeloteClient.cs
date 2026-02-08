using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Api.Hubs;

//Server -> Client
public interface IBeloteClient
{
    Task PlayerJoined(Lobby lobby);

    Task PlayerLeft(Lobby lobby);

    Task LobbyUpdated(Lobby lobby);

    Task LobbyDeleted(int lobbyId);

    Task GameStarted(Lobby lobby);

    Task CardsDealt(Lobby lobby, string dealerName, string bidderName);

    Task BidMade(Lobby lobby);
}
