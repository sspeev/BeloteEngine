using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Presentation.Models;

public sealed class LobbyResponseModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<PlayerResponseModel> ConnectedPlayers { get; init; } = [];
    public string GamePhase { get; init; } = string.Empty;
    public bool GameStarted { get; init; }
    public int PlayerCount { get; init; }

    public static LobbyResponseModel FromDomain(Lobby lobby)
    {
        return new LobbyResponseModel
        {
            Id = lobby.Id,
            Name = lobby.Name,
            ConnectedPlayers = lobby.ConnectedPlayers
                .Select(PlayerResponseModel.FromDomain)
                .ToArray(),
            GamePhase = lobby.GamePhase,
            GameStarted = lobby.GameStarted,
            PlayerCount = lobby.ConnectedPlayers.Count
        };
    }
}
