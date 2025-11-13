namespace WGetCrawler.Api.Models;

/// <summary>
/// 爬取规则定义
/// </summary>
public class CrawlRule
{
    /// <summary>
    /// 规则唯一标识
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 目标网站 URL
    /// </summary>
    public required string WebsiteUrl { get; set; }

    /// <summary>
    /// 输入框 CSS 选择器
    /// </summary>
    public required string InputSelector { get; set; }

    /// <summary>
    /// 提交按钮 CSS 选择器 (可选)
    /// </summary>
    public string? SubmitSelector { get; set; }

    /// <summary>
    /// 结果字段选择器映射
    /// </summary>
    public required Dictionary<string, string> ResultSelectors { get; set; }

    /// <summary>
    /// 等待时间(毫秒)
    /// </summary>
    public int WaitTime { get; set; } = 3000;

    /// <summary>
    /// 超时时间(毫秒)
    /// </summary>
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}
