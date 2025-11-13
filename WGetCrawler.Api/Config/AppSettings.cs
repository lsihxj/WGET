namespace WGetCrawler.Api.Config;

/// <summary>
/// 应用配置
/// </summary>
public class ApplicationOptions
{
    public int Port { get; set; } = 3000;
    public bool EnableScreenshot { get; set; } = false;
}

/// <summary>
/// 缓存配置
/// </summary>
public class CacheOptions
{
    public int CacheTtlSeconds { get; set; } = 604800; // 7天
    public int TaskRetentionMinutes { get; set; } = 30;
}

/// <summary>
/// 爬虫配置
/// </summary>
public class CrawlerOptions
{
    public int MaxConcurrent { get; set; } = 5;
    public int Timeout { get; set; } = 30000;
    public int RetryTimes { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;
    public bool Headless { get; set; } = true;
    public int BrowserPoolSize { get; set; } = 3;
}

/// <summary>
/// API 配置
/// </summary>
public class ApiOptions
{
    public string CorsOrigin { get; set; } = "*";
    public int RateLimit { get; set; } = 60;
}
