using System.ComponentModel.DataAnnotations;

namespace BeloteEngine.Api.Models
{
    public class RequestInfoModel
    {
        [Required(ErrorMessage = "Player name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string PlayerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lobby name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string LobbyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "LobbyId is required")]
        [Range(1, 9999)]
        public int LobbyId { get; set; }
    }
}
