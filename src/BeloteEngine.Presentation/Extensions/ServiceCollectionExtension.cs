using System.Text;
using System.Threading.RateLimiting;
using BeloteEngine.Application.Contracts;
using BeloteEngine.Application.Contracts.Auth;
using BeloteEngine.Application.Contracts.Caching;
using BeloteEngine.Application.Contracts.Lobby;
using BeloteEngine.Application.Contracts.Game;
using BeloteEngine.Application.Rules;
using BeloteEngine.Application.Services;
using BeloteEngine.Infrastructure.Auth;
using BeloteEngine.Infrastructure.Data;
using BeloteEngine.Infrastructure.Services;
using BeloteEngine.Presentation.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BeloteEngine.Application.User.Commands.Create;

namespace BeloteEngine.Presentation.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection service)
    {
        service.AddSingleton<ILobbyService, LobbyService>();
        service.AddSingleton<ILobbyStore, InMemoryLobbyStore>();
        service.AddSingleton<ILobbyJoinValidator, LobbyJoinValidator>();
        service.AddSingleton<ILobbyCreationValidator, LobbyCreationValidator>();
        service.AddSingleton<IGameService, GameService>();
        service.AddSingleton<IGameValidation, GameValidation>();
        service.AddSingleton<ITrickEvaluator, TrickEvaluator>();
        service.AddSingleton<IPlayValidator, PlayValidator>();
        service.AddSingleton<IScoreCalculator, ScoreCalculator>();
        service.AddSingleton<IAfkTimerService, AfkTimerService>();

        var applicationAssembly = typeof(CreateUserCommand).Assembly;
        service.AddMediatR(config =>
            config.RegisterServicesFromAssembly(applicationAssembly));

        return service;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddDbContext<BeloteEngineDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        service.AddSingleton<IJwtProvider, JwtProvider>();
        service.AddSingleton<ICachingService, CachingService>();
        service.AddScoped<IUserIdentityService, IdentityUserService>();

        return service;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection service)
    {
        service.AddIdentityCore<IdentityUser>(options =>
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
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!)),
                ClockSkew = TimeSpan.Zero  // Disable the default 5-minute grace period
            };
        });

        service.AddAuthorization();

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
