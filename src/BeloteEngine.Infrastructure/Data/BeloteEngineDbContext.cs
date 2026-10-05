
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeloteEngine.Infrastructure.Data;

public class BeloteEngineDbContext(DbContextOptions<BeloteEngineDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
}