namespace BeloteEngine.Api.Models
{
    public class LeaveRequestModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public int LobbyId { get; set; }
    }
}
