using WGetCrawler.Api.Config;
using WGetCrawler.Api.Services;
using WGetCrawler.Api.Middleware;
using AspNetCoreRateLimit;
using Serilog;

// 配置 Serilog 日志到文件和控制台
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: "logs/wget-crawler-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_485_760, // 10MB
        rollOnFileSizeLimit: true
    );

// 开发环境下同时输出到控制台
#if DEBUG
loggerConfig.WriteTo.Console(
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
);
#endif

Log.Logger = loggerConfig.CreateLogger();

try
{
    Log.Information("应用程序启动中...");

var builder = WebApplication.CreateBuilder(args);

// 使用 Serilog
builder.Host.UseSerilog();

// 配置选项
builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection("Application"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
builder.Services.Configure<CrawlerOptions>(builder.Configuration.GetSection("Crawler"));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 配置 CORS
var apiOptions = builder.Configuration.GetSection("Api").Get<ApiOptions>() ?? new ApiOptions();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (apiOptions.CorsOrigin == "*")
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(apiOptions.CorsOrigin.Split(','))
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// 配置速率限制
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = apiOptions.RateLimit
        }
    };
});

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// 注册服务
builder.Services.AddSingleton<RuleConfigService>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<CrawlerService>();
builder.Services.AddHostedService<TaskSchedulerService>();
builder.Services.AddSingleton<TaskSchedulerService>(sp => 
    sp.GetServices<IHostedService>().OfType<TaskSchedulerService>().First());

var app = builder.Build();

// 配置 HTTP 管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 使用自定义异常处理
app.UseMiddleware<ExceptionHandlerMiddleware>();

// 使用速率限制
app.UseIpRateLimiting();

// 使用 CORS
app.UseCors();

app.MapControllers();

// 初始化服务
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("正在初始化应用服务...");

// 初始化规则配置服务
var ruleService = app.Services.GetRequiredService<RuleConfigService>();
await ruleService.InitializeAsync();

// 初始化爬虫服务
var crawlerService = app.Services.GetRequiredService<CrawlerService>();
await crawlerService.InitializeAsync();

var appOptions = builder.Configuration.GetSection("Application").Get<ApplicationOptions>() ?? new ApplicationOptions();
logger.LogInformation("应用启动成功，监听端口: {Port}", appOptions.Port);
logger.LogInformation("访问 Swagger: http://localhost:{Port}/swagger", appOptions.Port);

// 配置监听端口
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{appOptions.Port}");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
