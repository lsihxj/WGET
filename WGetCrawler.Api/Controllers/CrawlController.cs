using Microsoft.AspNetCore.Mvc;
using WGetCrawler.Api.Models;
using WGetCrawler.Api.Services;

namespace WGetCrawler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlController : ControllerBase
{
    private readonly TaskSchedulerService _schedulerService;
    private readonly RuleConfigService _ruleService;
    private readonly ILogger<CrawlController> _logger;

    public CrawlController(
        TaskSchedulerService schedulerService,
        RuleConfigService ruleService,
        ILogger<CrawlController> logger)
    {
        _schedulerService = schedulerService;
        _ruleService = ruleService;
        _logger = logger;
    }

    /// <summary>
    /// 提交抓取任务
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitCrawlTask([FromBody] CrawlRequest request)
    {
        try
        {
            if (request.Models == null || request.Models.Count == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Message = "models 不能为空",
                    Data = null
                });
            }

            // 获取默认规则
            var rule = _ruleService.GetDefaultRule();
            if (rule == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Code = 400,
                    Message = "未找到可用的爬虫规则，请检查 rules.json 文件",
                    Data = null
                });
            }

            var taskId = await _schedulerService.SubmitTaskAsync(
                rule.Id,
                request.Models,
                request.UseCache,
                request.RetryTimes);

            return Ok(new ApiResponse<object>
            {
                Code = 200,
                Message = "任务已提交",
                Data = new
                {
                    TaskId = taskId,
                    Status = "processing",
                    TotalCount = request.Models.Count
                }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = ex.Message,
                Data = null
            });
        }
    }
}
