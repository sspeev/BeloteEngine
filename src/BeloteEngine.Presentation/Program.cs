using BeloteEngine.Infrastructure.Data;
using BeloteEngine.Presentation.Extensions;
using BeloteEngine.Presentation.Hubs;
using Microsoft.AspNetCore.Http.Connections;

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

builder.Services.AddSecurityServices(builder.Environment, builder.Configuration);
builder.AddPresentation();
builder.Services.AddSignalRConfiguration(builder.Environment);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddIdentityServices();

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
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
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
