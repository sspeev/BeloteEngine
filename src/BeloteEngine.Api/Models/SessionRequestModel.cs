using System.ComponentModel.DataAnnotations;

namespace BeloteEngine.Api.Models
{
    public class SessionRequestModel
    {
        [Required(ErrorMessage = "Player name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string PlayerName { get; set; } = string.Empty;

        public string? SessionId { get; set; }
    }
}
