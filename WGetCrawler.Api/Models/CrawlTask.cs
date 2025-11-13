namespace WGetCrawler.Api.Models;

/// <summary>
/// 任务状态枚举
/// </summary>
public enum TaskStatus
{
    Processing,
    Completed
}

/// <summary>
/// 爬取任务
/// </summary>
public class CrawlTask
{
    public required string TaskId { get; set; }
    public required string RuleId { get; set; }
    public required List<string> Models { get; set; }
    public TaskStatus Status { get; set; }
    public List<CrawlResult> Results { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public bool UseCache { get; set; } = true;
    public int RetryTimes { get; set; } = 3;
}

/// <summary>
/// 爬取结果
/// </summary>
public class CrawlResult
{
    public required string ModelNumber { get; set; }
    public string Status { get; set; } = "success";
    public Dictionary<string, string>? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public long Duration { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// 爬取请求
/// </summary>
public class CrawlRequest
{
    public required List<string> Models { get; set; }
    public bool UseCache { get; set; } = true;
    public int RetryTimes { get; set; } = 3;
}

/// <summary>
/// API 响应包装
/// </summary>
public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}
