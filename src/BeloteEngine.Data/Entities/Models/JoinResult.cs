using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Data.Entities.Models
{
    public class JoinResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public Lobby? Lobby { get; set; }
    }
}
