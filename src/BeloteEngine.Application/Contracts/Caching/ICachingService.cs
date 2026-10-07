
namespace BeloteEngine.Application.Contracts.Caching;

public interface ICachingService
{
    public T GetOrCreate<T>(string key);

    public void Set<T>(string key, T value, TimeSpan expiration);

    public void Remove(string key);

    public bool Exists(string key);
}