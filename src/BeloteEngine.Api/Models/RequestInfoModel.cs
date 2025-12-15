namespace BeloteEngine.Api.Models
{
    public class RequestInfoModel
    {
        public string PlayerName { get; set; } = string.Empty;

        public string LobbyName { get; set; } = string.Empty;

        public int LobbyId { get; set; }
    }
}
