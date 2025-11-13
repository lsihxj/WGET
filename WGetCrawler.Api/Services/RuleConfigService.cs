using System.Text.Json;
using WGetCrawler.Api.Models;

namespace WGetCrawler.Api.Services;

/// <summary>
/// 规则配置文件管理服务
/// </summary>
public class RuleConfigService
{
    private readonly string _configFilePath;
    private readonly ILogger<RuleConfigService> _logger;
    private List<CrawlRule> _rules = new();
    private FileSystemWatcher? _fileWatcher;
    private readonly object _lock = new();

    public RuleConfigService(ILogger<RuleConfigService> logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
    }

    /// <summary>
    /// 初始化配置文件和监听器
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadRulesAsync();
        StartFileWatcher();
    }

    /// <summary>
    /// 加载规则配置文件
    /// </summary>
    private async Task LoadRulesAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogWarning("规则配置文件不存在，创建默认空配置: {Path}", _configFilePath);
                await CreateDefaultConfigAsync();
                return;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize<RulesConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            lock (_lock)
            {
                _rules = config?.Rules?.Where(r => r.Enabled).ToList() ?? new List<CrawlRule>();
            }

            _logger.LogInformation("成功加载 {Count} 条规则", _rules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载规则配置失败，使用空规则列表");
            lock (_lock)
            {
                _rules = new List<CrawlRule>();
            }
        }
    }

    /// <summary>
    /// 创建默认配置文件
    /// </summary>
    private async Task CreateDefaultConfigAsync()
    {
        // 内置默认规则
        var defaultConfig = new RulesConfig
        {
            Rules = new List<CrawlRule>
            {
                new CrawlRule
                {
                    Id = "bom-ai-price",
                    Name = "bom.ai price",
                    WebsiteUrl = "https://www.bom.ai/components-storage/{modelNumber}.html",
                    InputSelector = "#kw",
                    SubmitSelector = "#su",
                    ResultSelectors = new Dictionary<string, string>
                    {
                        { "price", "#bomID_quotePrice_01" }
                    },
                    WaitTime = 500,
                    Timeout = 10000,
                    Enabled = true
                }
            }
        };
        
        var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_configFilePath, json);
        
        lock (_lock)
        {
            _rules = defaultConfig.Rules.Where(r => r.Enabled).ToList();
        }
        
        _logger.LogInformation("已创建默认规则配置文件，包含 {Count} 条规则", _rules.Count);
    }

    /// <summary>
    /// 启动文件监听器
    /// </summary>
    private void StartFileWatcher()
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (string.IsNullOrEmpty(directory))
            return;

        _fileWatcher = new FileSystemWatcher(directory)
        {
            Filter = "rules.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _fileWatcher.Changed += async (sender, e) =>
        {
            _logger.LogInformation("检测到规则配置文件变化，重新加载...");
            await Task.Delay(500); // 延迟避免文件锁定
            await LoadRulesAsync();
        };

        _fileWatcher.EnableRaisingEvents = true;
        _logger.LogInformation("启动规则配置文件监听: {Path}", _configFilePath);
    }

    /// <summary>
    /// 获取所有已启用的规则
    /// </summary>
    public List<CrawlRule> GetAllRules()
    {
        lock (_lock)
        {
            return new List<CrawlRule>(_rules);
        }
    }

    /// <summary>
    /// 根据ID获取规则
    /// </summary>
    public CrawlRule? GetRuleById(string id)
    {
        lock (_lock)
        {
            return _rules.FirstOrDefault(r => r.Id == id);
        }
    }

    /// <summary>
    /// 获取默认规则（第一个启用的规则）
    /// </summary>
    public CrawlRule? GetDefaultRule()
    {
        lock (_lock)
        {
            return _rules.FirstOrDefault();
        }
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }
}

/// <summary>
/// 规则配置文件根对象
/// </summary>
internal class RulesConfig
{
    public List<CrawlRule> Rules { get; set; } = new();
}
