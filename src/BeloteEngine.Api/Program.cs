using BeloteEngine.Api.Extensions;
using BeloteEngine.Api.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var cloudRunPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(cloudRunPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{cloudRunPort}");
}

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

builder.Services.AddDataProtection();

builder.Services.AddRateLimiter(options =>
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

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Belote Engine API", 
        Version = "v1",
        Description = "API for managing Belote game lobbies and gameplay"
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("*");
        }
        else
        {
            var allowedOrigins = builder.Configuration["AllowedOrigins"]
                ?? throw new InvalidOperationException("AllowedOrigins is not configured.");

            policy.SetIsOriginAllowed(origin =>
                  {
                      if (string.IsNullOrEmpty(origin)) return false;

                      var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim());
                      if (origins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
                      {
                          return true;
                      }

                      if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                      {
                          var host = uri.Host;
                          if (host.StartsWith("online-belote-", StringComparison.OrdinalIgnoreCase) &&
                              host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                          {
                              return true;
                          }
                      }

                      return false;
                  })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("*");
        }
    });
});
builder.Services.AddApplicationServices();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Debug);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
});
builder.Services.AddHealthChecks();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Belote Engine API v1.0...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Belote Engine API v1");
        c.RoutePrefix = "swagger";
        c.EnableTryItOutByDefault();
        c.InjectStylesheet("/swagger-ui/custom.css");
        c.DocumentTitle = "Belote Engine API Documentation";
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

/// Skip HTTPS redirection inside containers — TLS is terminated by the reverse proxy.
/// DOTNET_RUNNING_IN_CONTAINER is set automatically by the .NET Docker base image.
var isRunningInContainer = app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
if (!isRunningInContainer)
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapHealthChecks("/health");
app.UseRateLimiter();

// Global error handling endpoint
app.Map("/error", (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError("Unhandled exception occurred");
    return Results.Problem("An error occurred while processing your request");
});
app.MapControllers();
app.MapHub<BeloteHub>("/beloteHub", options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
});

try
{
    logger.LogInformation("Belote Engine API started successfully on {Urls}",
        string.Join(", ", app.Urls));
    
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application failed to start");
    throw;
}
finally
{
    logger.LogInformation("Belote Engine API shutting down...");
}
