using BeloteEngine.Data.Entities.Models;
using BeloteEngine.Services.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using static BeloteEngine.Services.Constants.LobbyConstants;

namespace BeloteEngine.Unit.Tests.Services;

public class LobbyServiceUnit
{
    [Fact]
    public void CreateLobby_ShouldCreateLobbyAndStoreIt()
    {
        //Arrange
        var expectedGame = new Game();
        var service = CreateService(() => expectedGame);

        //Act
        var lobby = service.CreateLobby("Test Lobby", "127.0.0.1");

        //Assert
        Assert.NotNull(lobby);
        Assert.NotEqual(0, lobby.Id);
        Assert.Equal("Test Lobby", lobby.Name);
        Assert.Same(expectedGame, lobby.Game);
        Assert.Same(lobby, service.GetLobby(lobby.Id));
        Assert.Empty(lobby.ConnectedPlayers);
    }

    [Fact]
    public void CreateLobby_ShouldRejectSecondLobbyFromSameIp_WhenLimitIsReached()
    {
        //Arrange
        var service = CreateService();
        _ = service.CreateLobby("First", "127.0.0.1");

        //Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => service.CreateLobby("Second", "127.0.0.1"));

        //Assert
        Assert.Equal(
            $"You can only create {MAX_LOBBIES_PER_IP} lobbies at a time.",
            exception.Message);
    }

    [Fact]
    public void CreateLobby_ShouldAllowNewLobbyAfterEmptyLobbyIsRemoved_ForSameIp()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("First", "127.0.0.1");
        var player = CreatePlayer("Host", lobby.Id, "session-1");

        //Act
        var joinResult = service.JoinLobby(player);
        var leftLobby = service.LeaveLobby(player, lobby.Id);
        var nextLobby = service.CreateLobby("Second", "127.0.0.1");

        //Assert
        Assert.True(joinResult.Success);
        Assert.True(leftLobby);
        Assert.NotNull(nextLobby);
    }

    [Fact]
    public void JoinLobby_ShouldReturnError_WhenLobbyIdIsMissing()
    {
        //Arrange
        var service = CreateService();
        var player = CreatePlayer("Host", null);

        //Act
        var result = service.JoinLobby(player);

        //Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid lobby ID.", result.ErrorMessage);
    }

    [Fact]
    public void JoinLobby_ShouldAddPlayer_WhenLobbyExists()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        var player = CreatePlayer("Host", lobby.Id, "session-1");

        //Act
        var result = service.JoinLobby(player);

        //Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Lobby);
        Assert.Single(result.Lobby.ConnectedPlayers);
        Assert.Equal("Host", result.Lobby.ConnectedPlayers[0].Name);
    }

    [Fact]
    public void JoinLobby_ShouldRejectDuplicateNameFromAnotherSession()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");

        //Act
        var firstPlayer = CreatePlayer("Host", lobby.Id, "session-1");
        _ = service.JoinLobby(firstPlayer);

        var duplicatePlayer = CreatePlayer("Host", lobby.Id, "session-2");
        var result = service.JoinLobby(duplicatePlayer);

        //Assert
        Assert.False(result.Success);
        Assert.Equal("Player name is already in use.", result.ErrorMessage);
    }

    private static Player CreatePlayer(string name, int? lobbyId, string sessionId = "session-default")
    {
        return new Player
        {
            Name = name,
            LobbyId = lobbyId,
            SessionId = sessionId,
            ConnectionId = "connection-1",
            Hoster = false
        };
    }
    
    private static LobbyService CreateService(Func<Game>? gameFactory = null)
    {
        var cachingService = new CachingService(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CachingService>.Instance);

        return new LobbyService(
            new FakeGameService(gameFactory),
            NullLogger<LobbyService>.Instance,
            cachingService);
    }
}
