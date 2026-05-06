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

    [Fact]
    public void JoinLobby_ShouldReturnError_WhenLobbyDoesNotExist()
    {
        //Arrange
        var service = CreateService();
        var player = CreatePlayer("Host", 9999, "session-1");

        //Act
        var result = service.JoinLobby(player);

        //Assert
        Assert.False(result.Success);
        Assert.Equal("Lobby 9999 does not exist.", result.ErrorMessage);
    }

    [Fact]
    public void JoinLobby_ShouldReconnectPlayer_WhenSameSessionRejoins()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        var player = CreatePlayer("Host", lobby.Id, "session-1");
        _ = service.JoinLobby(player);

        //Act
        var reconnecting = CreatePlayer("Host", lobby.Id, "session-1");
        reconnecting.ConnectionId = "new-connection";
        var result = service.JoinLobby(reconnecting);

        //Assert
        Assert.True(result.Success);
        Assert.Single(result.Lobby!.ConnectedPlayers);
    }

    [Fact]
    public void JoinLobby_ShouldReturnError_WhenLobbyIsFull()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        for (int i = 0; i < 4; i++)
            service.JoinLobby(CreatePlayer($"P{i}", lobby.Id, $"session-{i}"));

        //Act
        var result = service.JoinLobby(CreatePlayer("Extra", lobby.Id, "session-extra"));

        //Assert
        Assert.False(result.Success);
        Assert.Equal("Lobby is full.", result.ErrorMessage);
    }

    [Fact]
    public void LeaveLobby_ShouldReturnFalse_WhenLobbyDoesNotExist()
    {
        //Arrange
        var service = CreateService();
        var player = CreatePlayer("Ghost", 9999);

        //Act
        var result = service.LeaveLobby(player, 9999);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void LeaveLobby_ShouldRemovePlayer_WhenPlayerIsInLobby()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        var p1 = CreatePlayer("Alice", lobby.Id, "session-1");
        var p2 = CreatePlayer("Bob", lobby.Id, "session-2");
        service.JoinLobby(p1);
        service.JoinLobby(p2);

        //Act
        var result = service.LeaveLobby(p1, lobby.Id);

        //Assert
        Assert.True(result);
        Assert.Single(service.GetLobby(lobby.Id).ConnectedPlayers);
        Assert.Equal("Bob", service.GetLobby(lobby.Id).ConnectedPlayers[0].Name);
    }

    [Fact]
    public void LeaveLobby_ShouldDeleteLobby_WhenLastPlayerLeaves()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        var player = CreatePlayer("Solo", lobby.Id, "session-1");
        service.JoinLobby(player);

        //Act
        service.LeaveLobby(player, lobby.Id);

        //Assert
        Assert.Null(service.GetLobby(lobby.Id));
    }

    [Fact]
    public void IsFull_ShouldReturnFalse_WhenLobbyDoesNotExist()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.IsFull(9999);

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFull_ShouldReturnTrue_WhenLobbyHasFourPlayers()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        for (int i = 0; i < 4; i++)
            service.JoinLobby(CreatePlayer($"P{i}", lobby.Id, $"session-{i}"));

        //Act
        var result = service.IsFull(lobby.Id);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void GetAvailableLobbies_ShouldReturnOnlyNonFullNonStartedLobbies()
    {
        //Arrange
        var service = CreateService();
        var open = service.CreateLobby("Open", "10.0.0.1");
        service.JoinLobby(CreatePlayer("P1", open.Id, "s1"));

        var full = service.CreateLobby("Full", "10.0.0.2");
        for (int i = 0; i < 4; i++)
            service.JoinLobby(CreatePlayer($"F{i}", full.Id, $"sf{i}"));

        //Act
        var available = service.GetAvailableLobbies();

        //Assert
        Assert.Single(available);
        Assert.Equal("Open", available[0].Name);
        Assert.Equal(1, available[0].PlayerCount);
        Assert.False(available[0].IsFull);
    }

    [Fact]
    public void ResetLobby_ShouldClearPlayersAndResetState()
    {
        //Arrange
        var service = CreateService();
        var lobby = service.CreateLobby("Room", "127.0.0.1");
        service.JoinLobby(CreatePlayer("P1", lobby.Id, "s1"));
        service.JoinLobby(CreatePlayer("P2", lobby.Id, "s2"));

        //Act
        service.ResetLobby(lobby.Id);
        var updated = service.GetLobby(lobby.Id);

        //Assert
        Assert.NotNull(updated);
        Assert.Empty(updated.ConnectedPlayers);
        Assert.False(updated.GameStarted);
        Assert.Equal("waiting", updated.GamePhase);
    }

    [Fact]
    public void ResetLobby_ShouldDoNothing_WhenLobbyDoesNotExist()
    {
        //Arrange
        var service = CreateService();

        //Act & Assert — should not throw
        service.ResetLobby(9999);
    }

    [Fact]
    public void GetLobby_ShouldReturnNull_WhenLobbyDoesNotExist()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.GetLobby(9999);

        //Assert
        Assert.Null(result);
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
            new MockedGameService(gameFactory),
            NullLogger<LobbyService>.Instance,
            cachingService);
    }
}
