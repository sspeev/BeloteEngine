using System.ComponentModel.DataAnnotations;

namespace BeloteEngine.Api.Models
{
    public class LeaveRequestModel
    {
        [Required(ErrorMessage = "Player name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string PlayerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "LobbyId is required")]
        public int LobbyId { get; set; }
    }
}
