using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Rules;
using BeloteEngine.Application.Services;
using BeloteEngine.Domain.Entities.Models;
using BeloteEngine.Infrastructure.Data;
using BeloteEngine.Infrastructure.Session;
using BeloteEngine.Presentation.Services;
using Microsoft.EntityFrameworkCore;

namespace BeloteEngine.Presentation.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service)
    {
        service.AddSingleton<ILobbyService, LobbyService>();
        service.AddSingleton<IGameService, GameService>();
        service.AddSingleton<IConnectionLimiter, ConnectionLimiter>();
        service.AddSingleton<ITrickEvaluator, TrickEvaluator>();
        service.AddSingleton<IPlayValidator, PlayValidator>();
        service.AddSingleton<IScoreCalculator, ScoreCalculator>();
        service.AddSingleton<CachingService>();
        service.AddSingleton<IAfkTimerService, AfkTimerService>();
        service.AddSingleton<ISessionService, SessionService>();

        return service;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddDbContext<BeloteEngineDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        return service;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddIdentityApiEndpoints<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 4;
        })
        .AddEntityFrameworkStores<BeloteEngineDbContext>();
        return service;
    }
}
