using Microsoft.AspNetCore.Mvc;
using WGetCrawler.Api.Models;
using WGetCrawler.Api.Services;
using System.Diagnostics;

namespace WGetCrawler.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly RuleConfigService _ruleService;
    private readonly CacheService _cacheService;

    public HealthController(
        RuleConfigService ruleService,
        CacheService cacheService)
    {
        _ruleService = ruleService;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rules = _ruleService.GetAllRules();
        var currentProcess = Process.GetCurrentProcess();
        var cacheData = await _cacheService.GetAllCacheDataAsync();
        var cacheStats = await _cacheService.GetCacheStatsAsync();
        
        return Ok(new ApiResponse<object>
        {
            Code = 200,
            Message = "OK",
            Data = new
            {
                Status = "ok",
                ProcessName = currentProcess.ProcessName,
                RulesCount = rules.Count,
                Cache = new
                {
                    Stats = cacheStats,
                    Items = cacheData
                },
                Timestamp = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// 测试接口：添加测试缓存数据
    /// </summary>
    [HttpPost("test-cache")]
    public async Task<IActionResult> AddTestCache()
    {
        // 添加测试缓存数据（只有 price 字段）
        await _cacheService.SetAsync("crawl:LM324DR", new Dictionary<string, string>
        {
            { "price", "¥12.50" }
        });
        
        await _cacheService.SetAsync("crawl:NE555P", new Dictionary<string, string>
        {
            { "price", "¥3.20" }
        });
        
        await _cacheService.SetAsync("crawl:74HC595D", new Dictionary<string, string>
        {
            { "price", "¥1.85" }
        });

        return Ok(new ApiResponse<object>
        {
            Code = 200,
            Message = "测试缓存已添加",
            Data = new { Count = 3 }
        });
    }
}
