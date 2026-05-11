using BeloteEngine.Services.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeloteEngine.Unit.Tests.Services;

public class CachingServiceUnit
{
    [Fact]
    public void GetOrCreate_ShouldCallFactory_WhenKeyNotCached()
    {
        //Arrange
        var service = CreateService();
        var factoryCalled = false;

        //Act
        var result = service.GetOrCreate("key1", () =>
        {
            factoryCalled = true;
            return "value1";
        });

        //Assert
        Assert.True(factoryCalled);
        Assert.Equal("value1", result);
    }

    [Fact]
    public void GetOrCreate_ShouldReturnCachedValue_WhenKeyExists()
    {
        //Arrange
        var service = CreateService();
        service.GetOrCreate("key1", () => "first");
        var factoryCallCount = 0;

        //Act
        var result = service.GetOrCreate("key1", () =>
        {
            factoryCallCount++;
            return "second";
        });

        //Assert
        Assert.Equal("first", result);
        Assert.Equal(0, factoryCallCount);
    }

    [Fact]
    public void GetOrCreate_ShouldNotCache_WhenFactoryReturnsNull()
    {
        //Arrange
        var service = CreateService();
        service.GetOrCreate<string?>("key1", () => null);

        //Act
        var result = service.GetOrCreate("key1", () => "new-value");

        //Assert
        Assert.Equal("new-value", result);
    }

    [Fact]
    public void Remove_ShouldEvictCachedEntry()
    {
        //Arrange
        var service = CreateService();
        service.GetOrCreate("key1", () => "cached");

        //Act
        service.Remove("key1");
        var result = service.GetOrCreate("key1", () => "fresh");

        //Assert
        Assert.Equal("fresh", result);
    }

    [Fact]
    public void Exists_ShouldReturnTrue_WhenKeyIsCached()
    {
        //Arrange
        var service = CreateService();
        service.GetOrCreate("key1", () => "value");

        //Act
        var result = service.Exists("key1");

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void Exists_ShouldReturnFalse_WhenKeyNotCached()
    {
        //Arrange
        var service = CreateService();

        //Act
        var result = service.Exists("nonexistent");

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void Exists_ShouldReturnFalse_AfterRemove()
    {
        //Arrange
        var service = CreateService();
        service.GetOrCreate("key1", () => "value");
        service.Remove("key1");

        //Act
        var result = service.Exists("key1");

        //Assert
        Assert.False(result);
    }

    private static CachingService CreateService()
    {
        // SizeLimit must be set to honour Size = 1 in the cache entry options
        var options = new MemoryCacheOptions { SizeLimit = 1024 };
        return new CachingService(
            new MemoryCache(options),
            NullLogger<CachingService>.Instance);
    }
}
