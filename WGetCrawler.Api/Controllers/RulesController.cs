using Microsoft.AspNetCore.Mvc;
using WGetCrawler.Api.Models;
using WGetCrawler.Api.Services;

namespace WGetCrawler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly RuleConfigService _ruleService;

    public RulesController(RuleConfigService ruleService)
    {
        _ruleService = ruleService;
    }

    /// <summary>
    /// 获取所有已启用的规则
    /// </summary>
    [HttpGet]
    public IActionResult GetAllRules()
    {
        var rules = _ruleService.GetAllRules();
        
        return Ok(new ApiResponse<List<CrawlRule>>
        {
            Code = 200,
            Message = "成功",
            Data = rules
        });
    }

    /// <summary>
    /// 根据ID获取规则
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetRuleById(string id)
    {
        var rule = _ruleService.GetRuleById(id);
        
        if (rule == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Code = 404,
                Message = $"规则不存在: {id}",
                Data = null
            });
        }

        return Ok(new ApiResponse<CrawlRule>
        {
            Code = 200,
            Message = "成功",
            Data = rule
        });
    }
}
