using BeloteEngine.Data.Entities.Models;

namespace BeloteEngine.Data.Entities.Models
{
    public class JoinResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Lobby? Lobby { get; set; }
    }
}
