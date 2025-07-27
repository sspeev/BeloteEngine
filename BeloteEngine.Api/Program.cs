using BeloteEngine.Api.Hubs;
using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Contracts;
using BeloteEngine.Services.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameService, GameService>();

var app = builder.Build();

app.Logger.LogInformation("Starting BeloteEngine API...");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.EnableTryItOutByDefault();
        c.InjectStylesheet("/swagger-ui/custom.css");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWebSockets(); // Moved earlier in pipeline
app.UseCors("AllowFrontend");
app.UseRouting();
// Add Authentication if you plan to use it
// app.UseAuthentication();
app.UseAuthorization();

// Use endpoint routing for WebSockets
//app.MapGet("/ws/lobby", async context =>
//{
//    if (context.WebSockets.IsWebSocketRequest)
//    {
//        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
//        var playerId = context.Request.Query["playerId"];

//        // You need to implement this function
//        // await HandleWebSocketConnection(webSocket, playerId);
//    }
//    else
//    {
//        context.Response.StatusCode = 400;
//    }
//});

// Map endpoints
app.MapControllers();
app.MapHub<BeloteHub>("/beloteHub");
app.MapFallbackToFile("index.html");

await app.RunAsync();
