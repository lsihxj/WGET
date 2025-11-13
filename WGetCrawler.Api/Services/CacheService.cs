using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WGetCrawler.Api.Config;

namespace WGetCrawler.Api.Services;

/// <summary>
/// 缓存服务 - 使用内存缓存
/// </summary>
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _cacheKeys = new();

    public CacheService(
        IMemoryCache cache,
        IOptions<CacheOptions> options,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存对象
    /// </summary>
    public Task<T?> GetAsync<T>(string key)
    {
        var result = _cache.TryGetValue(key, out T? value) ? value : default;
        return Task.FromResult(result);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    public Task SetAsync<T>(string key, T value, int? ttlSeconds = null)
    {
        var expiration = TimeSpan.FromSeconds(ttlSeconds ?? _options.CacheTtlSeconds);
        var expirationTime = DateTime.UtcNow.Add(expiration);
        
        _cache.Set(key, value, expiration);
        _cacheKeys[key] = expirationTime;
        
        _logger.LogDebug("缓存已设置: {Key}, 过期时间: {Expiration}", key, expiration);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除指定缓存
    /// </summary>
    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _cacheKeys.TryRemove(key, out _);
        _logger.LogDebug("缓存已删除: {Key}", key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    public string GenerateKey(string ruleId, string modelNumber)
    {
        // 只有一个规则，缓存键中不需要包含规则ID
        return $"crawl:{modelNumber}";
    }

    /// <summary>
    /// 验证数据是否适合缓存
    /// </summary>
    public bool IsValidForCache(Dictionary<string, string>? data)
    {
        if (data == null || data.Count == 0)
            return false;

        // 至少有一个字段包含数字且长度大于1
        return data.Values.Any(v =>
            !string.IsNullOrWhiteSpace(v) &&
            v.Length > 1 &&
            v.Any(char.IsDigit) &&
            !IsOnlySymbols(v));
    }

    /// <summary>
    /// 检查是否只包含符号
    /// </summary>
    private bool IsOnlySymbols(string value)
    {
        var symbols = new[] { '¥', '$', '€', '£', '-', '_', '.', '*', ' ' };
        return value.All(c => symbols.Contains(c));
    }

    /// <summary>
    /// 获取所有缓存数据（用于监控和调试）
    /// </summary>
    public Task<List<object>> GetAllCacheDataAsync()
    {
        var now = DateTime.UtcNow;
        var result = new List<object>();

        // 清理过期的键
        var expiredKeys = _cacheKeys.Where(kv => kv.Value < now).Select(kv => kv.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _cacheKeys.TryRemove(key, out _);
        }

        // 获取有效缓存
        foreach (var kvp in _cacheKeys)
        {
            if (_cache.TryGetValue(kvp.Key, out Dictionary<string, string>? data) && data != null)
            {
                // 解析缓存键: crawl:型号
                var parts = kvp.Key.Split(':', 2);
                var modelNumber = parts.Length == 2 ? parts[1] : "unknown";

                result.Add(new
                {
                    ModelNumber = modelNumber,
                    Data = data,
                    ExpiresAt = kvp.Value,
                    RemainingSeconds = (int)(kvp.Value - now).TotalSeconds
                });
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public Task<object> GetCacheStatsAsync()
    {
        var now = DateTime.UtcNow;
        
        // 清理过期的键
        var expiredKeys = _cacheKeys.Where(kv => kv.Value < now).Select(kv => kv.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _cacheKeys.TryRemove(key, out _);
        }

        return Task.FromResult<object>(new
        {
            TotalCount = _cacheKeys.Count,
            TtlSeconds = _options.CacheTtlSeconds
        });
    }
}

