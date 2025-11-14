# WGetCrawler - .NET 8 Windows 可执行文件版本

## 项目概述

这是基于 .NET 8 平台重构的网页抓取系统,实现了极简部署的 Windows 可执行文件版本。

### 主要特性

- ✅ 自包含可执行文件,无需安装 .NET 运行时
- ✅ 无数据库依赖,使用 JSON 配置文件管理规则
- ✅ 内存缓存,提升重复查询性能
- ✅ 基于 PuppeteerSharp 的浏览器自动化
- ✅ 轻量级任务调度 (Channel)
- ✅ 完全兼容原有 API 接口

## 技术栈

- .NET 8 SDK
- ASP.NET Core Web API
- PuppeteerSharp 20.2.4 (浏览器自动化)
- IMemoryCache (内存缓存)
- Channel (任务队列)
- AspNetCoreRateLimit (速率限制)
- Serilog (结构化日志)

## 快速开始

### 1. 开发环境运行

```bash
cd WGetCrawler.Api
dotnet run
```

应用将在 `http://localhost:3000` 启动

### 2. 发布为可执行文件

运行发布脚本:

```bash
.\publish-win-x64.bat
```

或者手动执行:

```bash
cd WGetCrawler.Api
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../publish/win-x64
```

发布输出在 `publish/win-x64` 目录

### 3. 运行可执行文件

1. 进入发布目录: `cd publish/win-x64`
2. 双击 `WGetCrawler.Api.exe`
3. 首次运行会自动下载 Chromium 浏览器
4. 访问 `http://localhost:3000/swagger` 查看 API 文档

## 配置说明

### appsettings.json

```json
{
  "Application": {
    "Port": 3000,                // HTTP 监听端口
    "EnableScreenshot": false    // 开发环境截图开关
  },
  "Cache": {
    "CacheTtlSeconds": 604800,   // 缓存过期时间 (7天)
    "TaskRetentionMinutes": 30   // 任务保留时间 (30分钟)
  },
  "Crawler": {
    "MaxConcurrent": 5,          // 最大并发数
    "Timeout": 30000,            // 超时时间 (毫秒)
    "RetryTimes": 3,             // 默认重试次数
    "RetryDelay": 1000,          // 重试延迟 (毫秒)
    "Headless": true,            // 无头模式
    "BrowserPoolSize": 3         // 浏览器连接池大小
  },
  "Api": {
    "CorsOrigin": "*",           // CORS 允许来源
    "RateLimit": 60              // 速率限制 (请求/分钟)
  }
}
```

### rules.json

规则配置文件示例:

```json
{
  "rules": [
    {
      "id": "example-rule-001",
      "name": "示例网站价格查询",
      "websiteUrl": "https://example.com/search",
      "inputSelector": "#modelInput",
      "submitSelector": "button.search-btn",
      "resultSelectors": {
        "price": ".product-price",
        "stock": ".product-stock"
      },
      "waitTime": 3000,
      "timeout": 30000,
      "enabled": true
    }
  ]
}
```

**配置热重载**: 修改 `rules.json` 后自动生效,无需重启应用。

## API 接口

### 健康检查

```bash
GET /health
```

### 规则管理

```bash
# 获取所有规则
GET /api/rules

# 获取单个规则
GET /api/rules/{id}
```

### 抓取服务

```bash
# 提交抓取任务
POST /api/crawl
Content-Type: application/json

{
  "ruleId": "example-rule-001",
  "models": ["MODEL-001", "MODEL-002"],
  "useCache": true,
  "retryTimes": 3
}

# 查询任务状态
GET /api/tasks/{taskId}
```

## 项目结构

```
WGetCrawler.Api/
├── Config/              # 配置选项类
├── Controllers/         # API 控制器
├── Middleware/          # 中间件 (异常处理)
├── Models/              # 数据模型
├── Services/            # 核心服务
│   ├── CacheService.cs         # 缓存服务
│   ├── CrawlerService.cs       # 爬虫服务
│   ├── RuleConfigService.cs    # 规则配置管理
│   └── TaskSchedulerService.cs # 任务调度服务
├── Program.cs           # 应用入口
├── appsettings.json     # 应用配置
└── rules.json           # 规则配置
```

## 与 Node.js 版本的区别

| 特性 | Node.js 版本 | .NET 版本 |
|------|-------------|-----------|
| 数据库 | PostgreSQL | 无 (JSON 文件) |
| 缓存 | Redis | 内存缓存 |
| 任务队列 | BullMQ | Channel |
| 历史记录 | 持久化存储 | 仅保留 30 分钟 |
| 部署方式 | Docker / Node | 单文件可执行程序 |
| 依赖 | 需要 Node.js | 自包含运行时 |

## 注意事项

1. **任务记录**: 仅保留在内存中,应用重启后丢失
2. **缓存数据**: 应用重启后缓存清空
3. **规则管理**: 只能通过编辑 `rules.json` 文件,API 不提供写操作
4. **首次运行**: 需要联网下载 Chromium 浏览器 (~150MB)
5. **端口占用**: 确保 3000 端口未被占用

## 性能优化

- 浏览器连接池复用 (3个实例)
- 资源拦截 (图片/CSS/字体)
- 并行数据提取
- 缓存预检查
- 异步任务处理

## 故障排除

### 浏览器下载失败

PuppeteerSharp 会自动下载 Chromium 浏览器到用户目录:
- Windows: `%USERPROFILE%\.local-chromium\`

如果自动下载失败,可以手动下载并放置到该目录

### 端口已被占用

修改 `appsettings.json` 中的 `Application.Port` 配置

### 日志查看

所有日志输出到控制台,不记录文件

## 发布大小

- 可执行文件: ~80 MB
- Chromium 浏览器: ~150 MB
- 总磁盘占用: ~230 MB

## 许可证

MIT License
