using Microsoft.AspNetCore.Identity;

namespace BeloteEngine.Infrastructure.Data;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
