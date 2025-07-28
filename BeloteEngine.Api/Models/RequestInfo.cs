namespace BeloteEngine.Api.Models
{
    public class RequestInfo
    {
        public string PlayerName { get; set; } = string.Empty;

        public string LobbyName { get; set; } = string.Empty;

        public int LobbyId { get; set; }
    }
}
