using BeloteEngine.Services.Services;

namespace BeloteEngine.Unit.Tests.Services;

public class ConnectionLimiterUnit
{
    [Fact]
    public void CanConnect_ShouldReturnTrue_WhenNoConnectionsExist()
    {
        //Arrange
        var limiter = new ConnectionLimiter();

        //Act
        var result = limiter.CanConnect("192.168.1.1");

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void CanConnect_ShouldReturnTrue_WhenBelowMaxConnections()
    {
        //Arrange
        var limiter = new ConnectionLimiter();
        limiter.TrackConnection("192.168.1.1", "conn-1");
        limiter.TrackConnection("192.168.1.1", "conn-2");
        limiter.TrackConnection("192.168.1.1", "conn-3");

        //Act
        var result = limiter.CanConnect("192.168.1.1");

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void CanConnect_ShouldReturnFalse_WhenMaxConnectionsReached()
    {
        //Arrange
        var limiter = new ConnectionLimiter();
        limiter.TrackConnection("192.168.1.1", "conn-1");
        limiter.TrackConnection("192.168.1.1", "conn-2");
        limiter.TrackConnection("192.168.1.1", "conn-3");
        limiter.TrackConnection("192.168.1.1", "conn-4");

        //Act
        var result = limiter.CanConnect("192.168.1.1");

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void TrackConnection_ShouldAddConnectionForIp()
    {
        //Arrange
        var limiter = new ConnectionLimiter();

        //Act
        limiter.TrackConnection("192.168.1.1", "conn-1");
        limiter.TrackConnection("192.168.1.1", "conn-2");

        //Assert — still under limit
        Assert.True(limiter.CanConnect("192.168.1.1"));
    }

    [Fact]
    public void TrackConnection_ShouldNotDuplicateSameConnectionId()
    {
        //Arrange
        var limiter = new ConnectionLimiter();
        limiter.TrackConnection("192.168.1.1", "conn-1");

        //Act
        limiter.TrackConnection("192.168.1.1", "conn-1");

        //Assert — HashSet deduplicates, so only 1 tracked
        Assert.True(limiter.CanConnect("192.168.1.1"));
    }

    [Fact]
    public void RemoveConnection_ShouldDecrementCount_WhenConnectionRemoved()
    {
        //Arrange
        var limiter = new ConnectionLimiter();
        limiter.TrackConnection("192.168.1.1", "conn-1");
        limiter.TrackConnection("192.168.1.1", "conn-2");
        limiter.TrackConnection("192.168.1.1", "conn-3");
        limiter.TrackConnection("192.168.1.1", "conn-4");
        Assert.False(limiter.CanConnect("192.168.1.1"));

        //Act
        limiter.RemoveConnection("192.168.1.1", "conn-4");

        //Assert
        Assert.True(limiter.CanConnect("192.168.1.1"));
    }

    [Fact]
    public void RemoveConnection_ShouldCleanupIpEntry_WhenLastConnectionRemoved()
    {
        //Arrange
        var limiter = new ConnectionLimiter();
        limiter.TrackConnection("192.168.1.1", "conn-1");

        //Act
        limiter.RemoveConnection("192.168.1.1", "conn-1");

        //Assert — IP is cleaned up, next connect should be fine
        Assert.True(limiter.CanConnect("192.168.1.1"));
    }

    [Fact]
    public void RemoveConnection_ShouldDoNothing_WhenIpNotTracked()
    {
        //Arrange
        var limiter = new ConnectionLimiter();

        //Act & Assert — should not throw
        limiter.RemoveConnection("10.0.0.1", "conn-1");
        Assert.True(limiter.CanConnect("10.0.0.1"));
    }

    [Fact]
    public void CanConnect_ShouldTrackIpsIndependently()
    {
        //Arrange
        var limiter = new ConnectionLimiter();
        for (int i = 0; i < 4; i++)
            limiter.TrackConnection("192.168.1.1", $"conn-{i}");

        //Act & Assert
        Assert.False(limiter.CanConnect("192.168.1.1"));
        Assert.True(limiter.CanConnect("192.168.1.2"));
    }
}
