using System.Text;
using System.Threading.RateLimiting;
using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Rules;
using BeloteEngine.Application.Services;
using BeloteEngine.Infrastructure.Data;
using BeloteEngine.Infrastructure.Session;
using BeloteEngine.Presentation.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

    public static IServiceCollection AddIdentityServices(this IServiceCollection service)
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

    public static IServiceCollection AddSecurityServices(this IServiceCollection service, IHostEnvironment environment, IConfiguration configuration)
    {
        service.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("*");
                }
                else
                {
                    var allowedOrigins = configuration["AllowedOrigins"]
                        ?? throw new InvalidOperationException("AllowedOrigins is not configured.");

                    policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("*");
                }
            });
        });
        service.AddDataProtection();
        service.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;              // Max 100 requests
                limiterOptions.Window = TimeSpan.FromMinutes(1); // Per 1 minute
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;                 // Queue up to 2 requests
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken
                );
            };
        });

        service.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters.ValidIssuer = configuration["Jwt:Issuer"];
            options.TokenValidationParameters.ValidAudience = configuration["Jwt:Audience"];
            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        });

        return service;
    }

    public static IServiceCollection AddSignalRConfiguration(this IServiceCollection service, IHostEnvironment environment)
    {
        service.AddSignalR(options =>
        {
            options.EnableDetailedErrors = environment.IsDevelopment();
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.MaximumReceiveMessageSize = 102400; // 100 KB
        });
        return service;
    }

}
