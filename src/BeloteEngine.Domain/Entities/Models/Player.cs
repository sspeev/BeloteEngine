using System.Text.Json.Serialization;
using BeloteEngine.Domain.Entities.Enums;

namespace BeloteEngine.Domain.Entities.Models;

public class Player
{
    public required string UserId { get; set; }
    public required string Name { get; init; }
    public int? LobbyId { get; set; }
    public Status Status { get; set; } = Status.Disconnected;
    [JsonIgnore]
    public string ConnectionId { get; set; } = string.Empty;
    [JsonIgnore]
    public string SessionId { get; set; } = string.Empty;
    public bool Hoster { get; init; }
    public Announces AnnounceOffer { get; set; } = 0;
    public List<Card> Hand { get; set; } = [];

    //public int Combinations { get; set; }
}
