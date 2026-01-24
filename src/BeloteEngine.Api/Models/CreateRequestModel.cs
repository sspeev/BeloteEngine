using System.ComponentModel.DataAnnotations;

namespace BeloteEngine.Api.Models
{
    public class CreateRequestModel
    {
        [Required(ErrorMessage = "Player name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string PlayerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lobby name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string LobbyName { get; set; } = string.Empty;
    }
}
