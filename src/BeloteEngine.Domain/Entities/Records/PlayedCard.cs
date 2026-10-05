using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Domain.Entities.Records;

/// <summary>
/// Represents a single card played by a player during a trick.
/// Serialises as { "player": {...}, "card": {...} } — clean for client consumption.
/// </summary>
public record PlayedCard(Player Player, Card Card);
