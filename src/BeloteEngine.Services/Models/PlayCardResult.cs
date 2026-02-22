using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Services.Models;

public class PlayCardResult
{
    public Player? TrickWinner { get; set; }
    public bool RoundComplete { get; set; }
    public bool GameOver { get; set; }
}
