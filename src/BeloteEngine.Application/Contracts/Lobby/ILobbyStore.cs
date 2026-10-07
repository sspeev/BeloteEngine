using LobbyModel = BeloteEngine.Domain.Entities.Models.Lobby;

namespace BeloteEngine.Application.Contracts.Lobby;

public interface ILobbyStore
{
    int Count { get; }
    
    bool TryGet(int lobbyId, out LobbyModel? lobby);

    bool TryAdd(int lobbyId, LobbyModel lobby);

    bool TryRemove(int lobbyId, out LobbyModel? lobby);

    IReadOnlyCollection<LobbyModel> GetAll();
}
