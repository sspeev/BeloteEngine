using System.Text.Json.Serialization;
using BeloteEngine.Domain.Entities.Enums;

namespace BeloteEngine.Domain.Entities.Models;

public class Player
{
    /// <summary>
    /// Connection properties
    /// </summary>
    public required string UserId { get; set; }
    public required string Name { get; init; }
    public int LobbyId { get; set; }
    public bool Hoster { get; init; }
    public Status Status { get; set; } = Status.Disconnected;

    // [JsonIgnore]
    // public string ConnectionId { get; set; } = string.Empty;

    // [JsonIgnore]
    // public string SessionId { get; set; } = string.Empty;


    /// <summary>
    /// Game properties
    /// </summary>
    public Announces AnnounceOffer { get; set; } = 0;
    public List<Card> Hand { get; set; } = [];
    public Enums.Action Action { get; set; }
    public int? Combinations { get; set; }
}
