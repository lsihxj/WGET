using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using WGetCrawler.Api.Config;
using WGetCrawler.Api.Models;

namespace WGetCrawler.Api.Services;

/// <summary>
/// 任务调度服务 - 使用 Channel 实现轻量级任务队列
/// </summary>
public class TaskSchedulerService : BackgroundService
{
    private readonly CrawlerService _crawlerService;
    private readonly CacheService _cacheService;
    private readonly RuleConfigService _ruleService;
    private readonly CrawlerOptions _options;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<TaskSchedulerService> _logger;

    // 内存存储任务
    private readonly ConcurrentDictionary<string, CrawlTask> _tasks = new();
    
    // Channel 任务队列
    private readonly Channel<(string taskId, string model)> _taskChannel;
    
    // 并发控制
    private readonly SemaphoreSlim _concurrencyLimit;

    public TaskSchedulerService(
        CrawlerService crawlerService,
        CacheService cacheService,
        RuleConfigService ruleService,
        IOptions<CrawlerOptions> options,
        IOptions<CacheOptions> cacheOptions,
        ILogger<TaskSchedulerService> logger)
    {
        _crawlerService = crawlerService;
        _cacheService = cacheService;
        _ruleService = ruleService;
        _options = options.Value;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;

        // 创建有界 Channel
        _taskChannel = Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _concurrencyLimit = new SemaphoreSlim(_options.MaxConcurrent, _options.MaxConcurrent);
    }

    /// <summary>
    /// 提交批量任务
    /// </summary>
    public async Task<string> SubmitTaskAsync(
        string ruleId, 
        List<string> models, 
        bool useCache, 
        int retryTimes)
    {
        var rule = _ruleService.GetRuleById(ruleId);
        if (rule == null)
        {
            throw new ArgumentException($"规则不存在: {ruleId}");
        }

        if (retryTimes < 1 || retryTimes > 5)
        {
            throw new ArgumentException("重试次数必须在 1-5 之间");
        }

        var taskId = Guid.NewGuid().ToString();
        var task = new CrawlTask
        {
            TaskId = taskId,
            RuleId = ruleId,
            Models = models,
            Status = Models.TaskStatus.Processing,
            TotalCount = models.Count,
            CompletedCount = 0,
            CreatedAt = DateTime.UtcNow,
            UseCache = useCache,
            RetryTimes = retryTimes
        };

        _tasks[taskId] = task;

        // 检查缓存并写入队列
        foreach (var model in models)
        {
            if (useCache)
            {
                var cacheKey = _cacheService.GenerateKey(ruleId, model);
                var cached = await _cacheService.GetAsync<Dictionary<string, string>>(cacheKey);
                
                if (cached != null)
                {
                    task.Results.Add(new CrawlResult
                    {
                        ModelNumber = model,
                        Status = "success",
                        Data = cached,
                        Duration = 0,
                        RetryCount = 0
                    });
                    task.CompletedCount++;
                    _logger.LogDebug("缓存命中: {Model}", model);
                    continue;
                }
            }

            // 写入 Channel 队列
            await _taskChannel.Writer.WriteAsync((taskId, model));
        }

        _logger.LogInformation("任务已提交: {TaskId}, 型号数: {Count}, 缓存命中: {CacheHit}", 
            taskId, models.Count, task.CompletedCount);

        return taskId;
    }

    /// <summary>
    /// 获取任务状态和结果
    /// </summary>
    public Task<CrawlTask?> GetTaskStatusAsync(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }

    /// <summary>
    /// 后台任务处理循环
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("任务调度器已启动，并发数: {Concurrent}", _options.MaxConcurrent);

        // 启动多个消费者
        var consumers = Enumerable.Range(0, _options.MaxConcurrent)
            .Select(i => ProcessTasksAsync(i, stoppingToken))
            .ToArray();

        // 启动清理任务
        var cleanup = CleanupExpiredTasksAsync(stoppingToken);

        await Task.WhenAll(consumers.Concat(new[] { cleanup }));
    }

    /// <summary>
    /// 单个消费者任务处理
    /// </summary>
    private async Task ProcessTasksAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("消费者 {WorkerId} 已启动", workerId);

        await foreach (var (taskId, model) in _taskChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await _concurrencyLimit.WaitAsync(stoppingToken);
            
            try
            {
                if (!_tasks.TryGetValue(taskId, out var task))
                {
                    _logger.LogWarning("任务不存在: {TaskId}", taskId);
                    continue;
                }

                var rule = _ruleService.GetRuleById(task.RuleId);
                if (rule == null)
                {
                    _logger.LogError("规则不存在: {RuleId}", task.RuleId);
                    task.Results.Add(new CrawlResult
                    {
                        ModelNumber = model,
                        Status = "failed",
                        ErrorMessage = "规则不存在"
                    });
                    task.CompletedCount++;
                    continue;
                }

                _logger.LogDebug("消费者 {WorkerId} 开始处理: {Model}", workerId, model);

                // 执行爬取
                var result = await _crawlerService.CrawlAsync(rule, model, task.RetryTimes);
                
                // 保存结果
                task.Results.Add(result);
                task.CompletedCount++;

                // 缓存有效数据
                if (result.Status == "success" && result.Data != null && 
                    _cacheService.IsValidForCache(result.Data))
                {
                    var cacheKey = _cacheService.GenerateKey(task.RuleId, model);
                    await _cacheService.SetAsync(cacheKey, result.Data);
                }

                // 更新任务状态
                if (task.CompletedCount >= task.TotalCount)
                {
                    task.Status = Models.TaskStatus.Completed;
                    _logger.LogInformation("任务完成: {TaskId}, 成功: {Success}/{Total}", 
                        taskId, task.Results.Count(r => r.Status == "success"), task.TotalCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理任务失败: {TaskId}, {Model}", taskId, model);
            }
            finally
            {
                _concurrencyLimit.Release();
            }
        }
    }

    /// <summary>
    /// 清理过期任务 (每5分钟)
    /// </summary>
    private async Task CleanupExpiredTasksAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var expiredTime = DateTime.UtcNow.AddMinutes(-_cacheOptions.TaskRetentionMinutes);
            var expiredTasks = _tasks
                .Where(kvp => kvp.Value.Status == Models.TaskStatus.Completed && 
                             kvp.Value.CreatedAt < expiredTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var taskId in expiredTasks)
            {
                _tasks.TryRemove(taskId, out _);
            }

            if (expiredTasks.Count > 0)
            {
                _logger.LogInformation("已清理 {Count} 个过期任务", expiredTasks.Count);
            }
        }
    }
}
