using System.Diagnostics;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using WGetCrawler.Api.Config;
using WGetCrawler.Api.Models;

namespace WGetCrawler.Api.Services;

/// <summary>
/// 爬虫服务 - 使用 PuppeteerSharp 进行网页抓取
/// </summary>
public class CrawlerService : IDisposable
{
    private readonly CrawlerOptions _options;
    private readonly ApplicationOptions _appOptions;
    private readonly ILogger<CrawlerService> _logger;
    private readonly List<IBrowser> _browserPool = new();
    private int _currentBrowserIndex = 0;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private bool _initialized = false;

    public CrawlerService(
        IOptions<CrawlerOptions> options,
        IOptions<ApplicationOptions> appOptions,
        ILogger<CrawlerService> logger)
    {
        _options = options.Value;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// 初始化浏览器连接池
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _logger.LogInformation("初始化 PuppeteerSharp 浏览器连接池...");
            
            // 检查并下载浏览器
            var browserPath = await EnsureBrowserInstalledAsync();

            // 创建浏览器池
            var launchOptions = new LaunchOptions
            {
                Headless = _options.Headless,
                ExecutablePath = browserPath,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-blink-features=AutomationControlled",
                    "--disable-web-security",
                    "--disable-features=IsolateOrigins,site-per-process",
                    "--disable-gpu",
                    "--no-first-run",
                    "--no-default-browser-check",
                    "--disable-extensions",
                    "--ignore-certificate-errors"
                },
                IgnoredDefaultArgs = new[] { "--enable-automation" }  // 隐藏自动化标识
            };

            for (int i = 0; i < _options.BrowserPoolSize; i++)
            {
                var browser = await Puppeteer.LaunchAsync(launchOptions);
                _browserPool.Add(browser);
                _logger.LogInformation("浏览器实例 {Index} 已创建", i + 1);
            }

            _initialized = true;
            _logger.LogInformation("浏览器连接池初始化完成，共 {Count} 个实例", _browserPool.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化浏览器连接池失败");
            throw;
        }
    }

    /// <summary>
    /// 检查并安装 Chromium 浏览器
    /// </summary>
    private async Task<string> EnsureBrowserInstalledAsync()
    {
        // 1. 先尝试检测系统中已安装的 Chrome/Chromium
        var systemBrowserPath = FindSystemChrome();
        if (!string.IsNullOrEmpty(systemBrowserPath))
        {
            _logger.LogInformation("检测到系统中已安装的 Chrome/Chromium: {Path}", systemBrowserPath);
            return systemBrowserPath;
        }

        // 2. 检查 PuppeteerSharp 下载的浏览器
        var browserFetcher = new BrowserFetcher();
        var installedBrowsers = browserFetcher.GetInstalledBrowsers();
        
        if (installedBrowsers.Any())
        {
            var browser = installedBrowsers.First();
            var path = browser.GetExecutablePath();
            _logger.LogInformation("PuppeteerSharp 浏览器已安装: BuildId {BuildId}, Path: {Path}", 
                browser.BuildId, path);
            return path;
        }

        // 3. 都没有，则下载浏览器
        _logger.LogInformation("未检测到可用的 Chrome/Chromium，开始下载...");
        _logger.LogInformation("首次运行需要下载浏览器（约 150MB），请耐心等待...");
        
        var downloadedBrowser = await browserFetcher.DownloadAsync();
        var downloadedPath = downloadedBrowser.GetExecutablePath();
        
        _logger.LogInformation("Chromium 浏览器下载完成: BuildId {BuildId}, Path: {Path}", 
            downloadedBrowser.BuildId, downloadedPath);
        
        return downloadedPath;
    }

    /// <summary>
    /// 查找系统中已安装的 Chrome/Chromium
    /// </summary>
    private string? FindSystemChrome()
    {
        // Windows 常见的 Chrome/Chromium 安装路径
        var possiblePaths = new[]
        {
            // Chrome 标准安装路径
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            
            // Chromium 标准安装路径
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Chromium", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Chromium", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chromium", "Application", "chrome.exe"),
            
            // Edge (基于 Chromium)
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("找到浏览器: {Path}", path);
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// 执行单次抓取
    /// </summary>
    public async Task<CrawlResult> CrawlAsync(CrawlRule rule, string modelNumber, int retryTimes)
    {
        var stopwatch = Stopwatch.StartNew();
        var attemptCount = 0;
        Exception? lastException = null;

        for (int i = 0; i <= retryTimes; i++)
        {
            attemptCount++;
            try
            {
                var result = await PerformCrawlAsync(rule, modelNumber);
                stopwatch.Stop();
                
                result.Duration = stopwatch.ElapsedMilliseconds;
                result.RetryCount = attemptCount - 1;
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "抓取失败 (尝试 {Attempt}/{Total}): {Model}", 
                    attemptCount, retryTimes + 1, modelNumber);

                if (i < retryTimes)
                {
                    var delay = _options.RetryDelay * (i + 1);
                    await Task.Delay(delay);
                }
            }
        }

        stopwatch.Stop();
        return new CrawlResult
        {
            ModelNumber = modelNumber,
            Status = "failed",
            ErrorMessage = lastException?.Message ?? "抓取失败",
            Duration = stopwatch.ElapsedMilliseconds,
            RetryCount = attemptCount - 1
        };
    }

    /// <summary>
    /// 执行实际的抓取操作
    /// </summary>
    private async Task<CrawlResult> PerformCrawlAsync(CrawlRule rule, string modelNumber)
    {
        IBrowser browser = await GetBrowserAsync();
        var page = await browser.NewPageAsync();

        try
        {
            // 设置 User-Agent
            await page.SetUserAgentAsync(GetRandomUserAgent());

            // 检查是否需要直接访问详情页
            // 如果 WebsiteUrl 包含 {modelNumber} 占位符，则直接访问详情页
            string targetUrl;
            bool directAccess = rule.WebsiteUrl.Contains("{modelNumber}");
            
            if (directAccess)
            {
                // 直接访问详情页模式
                targetUrl = rule.WebsiteUrl.Replace("{modelNumber}", modelNumber);
                _logger.LogInformation("直接访问详情页模式: {Url}", targetUrl);
                
                try
                {
                    var response = await page.GoToAsync(targetUrl, new NavigationOptions 
                    { 
                        WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                        Timeout = rule.Timeout > 0 ? rule.Timeout : 10000
                    });
                    
                    if (response == null || !response.Ok)
                    {
                        throw new Exception($"页面加载失败: HTTP {response?.Status}");
                    }
                    
                    _logger.LogInformation("✅ 详情页加载成功");
                }
                catch (NavigationException navEx)
                {
                    _logger.LogWarning(navEx, "导航异常，尝试简化导航选项");
                    // 如果导航失败，尝试不带选项的简单导航
                    var response = await page.GoToAsync(targetUrl);
                    if (response == null || !response.Ok)
                    {
                        throw new Exception($"页面加载失败: HTTP {response?.Status}");
                    }
                }
                
                // 直接等待价格元素，不等待网络空闲
                // 这样更快，因为只要价格元素出现就可以提取了
            }
            else
            {
                // 传统搜索模式
                targetUrl = rule.WebsiteUrl;
                _logger.LogDebug("开始导航到 {Url}", targetUrl);
                var response = await page.GoToAsync(targetUrl);
                
                if (response == null || !response.Ok)
                {
                    throw new Exception($"页面加载失败: HTTP {response?.Status}");
                }
                
                _logger.LogDebug("页面加载成功，等待网络空闲...");
                try
                {
                    await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = 5000 });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "等待网络空闲超时，继续执行");
                }

                // 截图 (开发环境)
                if (_appOptions.EnableScreenshot)
                {
                    var screenshotPath = $"./logs/screenshot-{modelNumber}-initial-{DateTime.Now:yyyyMMddHHmmss}.png";
                    Directory.CreateDirectory("./logs");
                    await page.ScreenshotAsync(screenshotPath);
                    _logger.LogDebug("已保存初始页面截图: {Path}", screenshotPath);
                }

                // 等待输入框
                _logger.LogDebug("等待输入框: {Selector}", rule.InputSelector);
                try
                {
                    await page.WaitForSelectorAsync(rule.InputSelector, new WaitForSelectorOptions { Timeout = 10000 });
                    _logger.LogInformation("✅ 找到输入框: {Selector}", rule.InputSelector);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 未找到输入框: {Selector}，请检查选择器是否正确", rule.InputSelector);
                    throw new Exception($"未找到输入框: {rule.InputSelector}，页面可能已改版或选择器不正确", ex);
                }

                // 填入型号
                _logger.LogDebug("输入型号: {ModelNumber}", modelNumber);
                await page.TypeAsync(rule.InputSelector, modelNumber);
                await Task.Delay(500);
                _logger.LogInformation("✅ 已输入型号: {ModelNumber}", modelNumber);

                // 提交 (如果有提交按钮)
                if (!string.IsNullOrWhiteSpace(rule.SubmitSelector))
                {
                    _logger.LogDebug("等待并点击提交按钮: {Selector}", rule.SubmitSelector);
                    try
                    {
                        await page.WaitForSelectorAsync(rule.SubmitSelector, new WaitForSelectorOptions { Timeout = 5000 });
                        _logger.LogInformation("✅ 找到提交按钮: {Selector}", rule.SubmitSelector);
                        
                        var urlBeforeClick = page.Url;
                        _logger.LogInformation("点击前 URL: {Url}", urlBeforeClick);
                        
                        await page.ClickAsync(rule.SubmitSelector);
                        _logger.LogInformation("✅ 已点击搜索按钮");
                        
                        await Task.Delay(2000);
                        
                        var urlAfterClick = page.Url;
                        _logger.LogInformation("点击后 URL: {Url}", urlAfterClick);
                        
                        if (urlAfterClick.Contains("components-storage"))
                        {
                            _logger.LogInformation("✅ 已直接跳转到详情页");
                            await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = 5000 });
                        }
                        else
                        {
                            _logger.LogInformation("未直接跳转到详情页，分析页面内容...");
                            
                            var pageAnalysis = await page.EvaluateFunctionAsync<string>(@"() => {
                                return JSON.stringify({
                                    currentUrl: window.location.href,
                                    hasComponentsLinks: document.querySelectorAll('a[href*=""components-storage""]').length,
                                    visibleLinks: Array.from(document.querySelectorAll('a[href*=""components-storage""]'))
                                        .filter(el => el.offsetParent !== null)
                                        .slice(0, 5)
                                        .map(el => ({
                                            href: el.href,
                                            text: el.textContent?.trim().substring(0, 50)
                                        }))
                                }, null, 2);
                            }");
                            
                            _logger.LogInformation("页面分析:\n{Analysis}", pageAnalysis);
                            
                            var detailLink = await page.QuerySelectorAsync("a[href*='components-storage']");
                            if (detailLink != null)
                            {
                                var linkHref = await page.EvaluateFunctionAsync<string>("el => el.href", detailLink);
                                _logger.LogInformation("找到详情页链接: {Href}，准备点击", linkHref);
                                
                                await detailLink.ClickAsync();
                                _logger.LogInformation("已点击详情页链接，等待跳转...");
                                
                                var maxWait = 10;
                                for (int i = 0; i < maxWait; i++)
                                {
                                    await Task.Delay(1000);
                                    if (page.Url.Contains("components-storage"))
                                    {
                                        _logger.LogInformation("✅ 已跳转到详情页: {Url}", page.Url);
                                        break;
                                    }
                                }
                                
                                if (!page.Url.Contains("components-storage"))
                                {
                                    _logger.LogWarning("等待详情页超时，当前URL: {Url}", page.Url);
                                }
                                
                                await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = 5000 });
                            }
                            else
                            {
                                _logger.LogWarning("未找到详情页链接");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ 搜索流程失败: {Selector}", rule.SubmitSelector);
                        throw new Exception($"搜索流程失败: {rule.SubmitSelector}", ex);
                    }
                }
                else
                {
                    _logger.LogDebug("按下 Enter 键提交");
                    await page.Keyboard.PressAsync("Enter");
                    _logger.LogInformation("✅ 已按下 Enter 键");
                    await Task.Delay(2000);
                }
            }

            // 直接访问模式：跳过不必要的等待和分析
            if (!directAccess)
            {
                // 额外等待结果面板渲染完成（仅搜索模式需要）
                _logger.LogDebug("等待搜索结果面板渲染 {WaitTime}ms...", rule.WaitTime);
                await Task.Delay(rule.WaitTime);
            }
            
            // 记录当前页面信息（仅 Debug 模式）
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("当前页面 URL: {Url}", page.Url);
                _logger.LogDebug("当前页面标题: {Title}", await page.GetTitleAsync());
            }
            
            // 截图（仅开发环境且非直接访问模式）
            if (_appOptions.EnableScreenshot && !directAccess)
            {
                var screenshotPath = $"./logs/screenshot-{modelNumber}-result-{DateTime.Now:yyyyMMddHHmmss}.png";
                Directory.CreateDirectory("./logs");
                await page.ScreenshotAsync(screenshotPath);
                _logger.LogDebug("已保存搜索结果页面截图: {Path}", screenshotPath);
            }
            
            // 尝试等待结果元素出现（取第一个结果选择器作为等待目标）
            if (rule.ResultSelectors.Any())
            {
                var firstResultSelector = rule.ResultSelectors.First().Value;
                _logger.LogDebug("等待价格元素出现: {Selector}", firstResultSelector);
                try
                {
                    await page.WaitForSelectorAsync(firstResultSelector, new WaitForSelectorOptions 
                    { 
                        Timeout = 8000  // 减少到8秒
                    });
                    _logger.LogDebug("✅ 价格元素已出现");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "❌ 等待价格元素超时: {Selector}", firstResultSelector);
                }
            }

            // 提取数据（减少重试次数）
            _logger.LogDebug("开始提取数据");
            var data = await ExtractDataAsync(page, rule.ResultSelectors, retryTimes: 1);

            return new CrawlResult
            {
                ModelNumber = modelNumber,
                Status = "success",
                Data = data
            };
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// 提取页面数据 (支持重试)
    /// </summary>
    private async Task<Dictionary<string, string>> ExtractDataAsync(
        IPage page, 
        Dictionary<string, string> selectors, 
        int retryTimes)
    {
        Dictionary<string, string>? result = null;
        
        for (int attempt = 0; attempt <= retryTimes; attempt++)
        {
            _logger.LogDebug("数据提取尝试 {Attempt}/{Total}", attempt + 1, retryTimes + 1);
            
            // 并行提取所有字段
            var tasks = selectors.Select(async kvp =>
            {
                try
                {
                    _logger.LogDebug("查找元素: {Key} => {Selector}", kvp.Key, kvp.Value);
                    var element = await page.QuerySelectorAsync(kvp.Value);
                    
                    if (element == null)
                    {
                        _logger.LogWarning("未找到元素: {Key} => {Selector}", kvp.Key, kvp.Value);
                        return (kvp.Key, string.Empty);
                    }
                    
                    var text = await page.EvaluateFunctionAsync<string>("el => el.textContent", element);
                    var trimmedText = text?.Trim() ?? string.Empty;
                    _logger.LogDebug("提取到数据: {Key} = '{Value}'", kvp.Key, trimmedText);
                    return (kvp.Key, trimmedText);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "提取数据失败: {Key} => {Selector}", kvp.Key, kvp.Value);
                    return (kvp.Key, string.Empty);
                }
            });

            var extractedData = await Task.WhenAll(tasks);
            result = extractedData.ToDictionary(x => x.Key, x => x.Item2);

            _logger.LogDebug("提取结果: {Data}", System.Text.Json.JsonSerializer.Serialize(result));

            // 验证数据
            if (IsValidData(result))
            {
                _logger.LogDebug("数据验证通过");
                return result;
            }

            if (attempt < retryTimes)
            {
                var delay = 1000 + (attempt * 300);  // 减少重试延迟：1s, 1.3s
                _logger.LogDebug("数据验证失败，{Delay}ms 后重试 (尝试 {Attempt}/{Total})", 
                    delay, attempt + 2, retryTimes + 1);
                await Task.Delay(delay);
            }
            else
            {
                _logger.LogWarning("数据验证失败，已达最大重试次数");
            }
        }

        return result ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// 验证提取的数据
    /// </summary>
    private bool IsValidData(Dictionary<string, string> data)
    {
        if (data == null || data.Count == 0) return false;

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
    /// 从连接池获取浏览器实例 (轮询)
    /// </summary>
    private async Task<IBrowser> GetBrowserAsync()
    {
        await _browserLock.WaitAsync();
        try
        {
            var browser = _browserPool[_currentBrowserIndex];
            _currentBrowserIndex = (_currentBrowserIndex + 1) % _browserPool.Count;
            return browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// 获取随机 User-Agent
    /// </summary>
    private string GetRandomUserAgent()
    {
        var userAgents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };
        return userAgents[Random.Shared.Next(userAgents.Length)];
    }

    /// <summary>
    /// 关闭所有浏览器实例
    /// </summary>
    public async Task CloseAsync()
    {
        foreach (var browser in _browserPool)
        {
            await browser.CloseAsync();
        }
        _browserPool.Clear();
        _logger.LogInformation("浏览器连接池已关闭");
    }

    public void Dispose()
    {
        CloseAsync().GetAwaiter().GetResult();
        _browserLock.Dispose();
    }
}
