using BeloteEngine.Domain.Entities.Models;

namespace BeloteEngine.Application.DTOs;

public class PlayCardResult
{
    public Player? TrickWinner { get; set; }
    public bool RoundComplete { get; set; }
    public bool GameOver { get; set; }
}
