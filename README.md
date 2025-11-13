# WGetCrawler - 电子元器件价格查询系统

<div align="center">

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey.svg)](https://github.com)

一个高性能、易用的电子元器件价格查询工具，基于 .NET 8.0 和 PuppeteerSharp 构建。

[快速开始](#-快速开始) • [API文档](#-api-接口) • [配置说明](#️-配置说明) • [常见问题](#-常见问题)

</div>

---

## ✨ 特性

- 🚀 **高性能** - 单次查询 2-3 秒，性能优化 60-70%
- 📦 **独立部署** - 单文件可执行程序，无需安装 .NET Runtime
- 🎯 **精准查询** - 直接访问详情页，准确提取价格数据
- 🔄 **批量处理** - 支持一次查询多个型号，并发处理
- 💾 **智能缓存** - 7天缓存机制，提升重复查询速度
- 🔧 **易于集成** - RESTful API，支持多种编程语言调用
- 📊 **可视化文档** - 内置 Swagger UI，交互式 API 文档
- 🛡️ **稳定可靠** - 自动重试、错误处理、详细日志

## 🎯 适用场景

- 电子元器件采购价格对比
- 自动化询价系统集成
- 价格监控和趋势分析
- 批量元器件价格查询

## 📋 系统要求

- **操作系统**: Windows x64 (Windows 10/11 或 Windows Server 2016+)
- **内存**: 最低 2GB RAM
- **磁盘**: 约 250MB 可用空间（含浏览器）
- **网络**: 需要访问 bom.ai 网站

## 🚀 快速开始

### 方式一：使用发布包（推荐）

1. **下载并解压**
   ```bash
   # 解压 WGetCrawler-v1.0.0-Windows-x64.zip
   ```

2. **启动程序**
   ```bash
   # 双击 "启动程序.bat"
   # 或在命令行中运行
   SPDWGetCrawler.Api.exe
   ```

3. **等待启动**
   - 首次运行会自动下载 Chromium 浏览器（约 150MB）
   - 看到 "Application started" 表示启动成功

4. **测试功能**
   ```powershell
   # 运行测试脚本
   .\测试API.ps1
   
   # 或访问 API 文档
   # http://localhost:3000/swagger
   ```

### 方式二：从源码构建

1. **克隆仓库**
   ```bash
   git clone <repository-url>
   cd wGet
   ```

2. **还原依赖**
   ```bash
   cd WGetCrawler.Api
   dotnet restore
   ```

3. **运行开发环境**
   ```bash
   dotnet run
   ```

4. **发布为可执行文件**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
   ```

## 📡 API 接口

### 1. 健康检查

**请求**
```http
GET /health
```

**响应**
```json
{
  "code": 200,
  "message": "OK",
  "data": {
    "status": "ok",
    "rulesCount": 1,
    "timestamp": "2025-11-13T04:49:17Z"
  }
}
```

### 2. 提交查询任务

**请求**
```http
POST /api/Crawl
Content-Type: application/json

{
  "models": ["STM32F103C8T6", "STM32F407VET6"],
  "useCache": true,
  "retryTimes": 3
}
```

**参数说明**
| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| models | string[] | ✅ | - | 要查询的型号列表 |
| useCache | boolean | ❌ | true | 是否使用缓存 |
| retryTimes | int | ❌ | 3 | 失败重试次数 |

**响应**
```json
{
  "code": 200,
  "message": "任务已提交",
  "data": {
    "taskId": "0c2939ee-ca7d-402b-8fe6-4d9ee8a7021b",
    "status": "processing",
    "totalCount": 2
  }
}
```

### 3. 查询任务结果

**请求**
```http
GET /api/Tasks/{taskId}
```

**响应**
```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "taskId": "0c2939ee-ca7d-402b-8fe6-4d9ee8a7021b",
    "status": "completed",
    "totalCount": 2,
    "completedCount": 2,
    "results": [
      {
        "modelNumber": "STM32F103C8T6",
        "status": "success",
        "data": {
          "price": "4.35"
        },
        "duration": 2156,
        "retryCount": 0
      },
      {
        "modelNumber": "STM32F407VET6",
        "status": "success",
        "data": {
          "price": "12.80"
        },
        "duration": 2345,
        "retryCount": 0
      }
    ]
  }
}
```

### 4. 获取爬虫规则

**请求**
```http
GET /api/Rules
```

**响应**
```json
{
  "code": 200,
  "message": "成功",
  "data": [
    {
      "id": "bom-ai-price",
      "name": "BOM.ai 价格查询",
      "websiteUrl": "https://www.bom.ai/components-storage/{modelNumber}.html",
      "enabled": true
    }
  ]
}
```

## 💻 编程示例

### PowerShell

```powershell
# 1. 提交查询任务
$task = Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" `
    -ContentType "application/json" `
    -Body '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 2. 等待3秒
Start-Sleep 3

# 3. 查询结果
$result = Invoke-RestMethod -Uri "http://localhost:3000/api/Tasks/$($task.data.taskId)"

# 4. 提取价格
$price = $result.data.results[0].data.price
Write-Host "价格: ¥$price"
```

### Python

```python
import requests
import time

# 1. 提交查询任务
response = requests.post(
    "http://localhost:3000/api/Crawl",
    json={
        "models": ["STM32F103C8T6"],
        "useCache": True,
        "retryTimes": 3
    }
)
task_id = response.json()["data"]["taskId"]

# 2. 等待3秒
time.sleep(3)

# 3. 查询结果
result = requests.get(f"http://localhost:3000/api/Tasks/{task_id}")
price = result.json()["data"]["results"][0]["data"]["price"]
print(f"价格: ¥{price}")
```

### JavaScript (Node.js)

```javascript
const axios = require('axios');

async function queryPrice(modelNumber) {
  // 1. 提交查询任务
  const response = await axios.post('http://localhost:3000/api/Crawl', {
    models: [modelNumber],
    useCache: true,
    retryTimes: 3
  });
  
  const taskId = response.data.data.taskId;
  
  // 2. 等待3秒
  await new Promise(resolve => setTimeout(resolve, 3000));
  
  // 3. 查询结果
  const result = await axios.get(`http://localhost:3000/api/Tasks/${taskId}`);
  const price = result.data.data.results[0].data.price;
  
  console.log(`价格: ¥${price}`);
}

queryPrice('STM32F103C8T6');
```

### C#

```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:3000") };

// 1. 提交查询任务
var request = new { models = new[] { "STM32F103C8T6" }, useCache = true, retryTimes = 3 };
var response = await client.PostAsJsonAsync("/api/Crawl", request);
var taskData = await response.Content.ReadFromJsonAsync<dynamic>();
var taskId = taskData.data.taskId;

// 2. 等待3秒
await Task.Delay(3000);

// 3. 查询结果
var result = await client.GetFromJsonAsync<dynamic>($"/api/Tasks/{taskId}");
var price = result.data.results[0].data.price;
Console.WriteLine($"价格: ¥{price}");
```

### cURL

```bash
# 1. 提交查询任务
curl -X POST http://localhost:3000/api/Crawl \
  -H "Content-Type: application/json" \
  -d '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 2. 查询结果（替换 {taskId}）
curl http://localhost:3000/api/Tasks/{taskId}
```

## ⚙️ 配置说明

### appsettings.json

```json
{
  "Application": {
    "Port": 3000,              // HTTP 监听端口
    "EnableScreenshot": false  // 开发环境截图开关
  },
  "Cache": {
    "CacheTtlSeconds": 604800,   // 缓存过期时间 (7天)
    "TaskRetentionMinutes": 30   // 任务保留时间 (30分钟)
  },
  "Crawler": {
    "MaxConcurrent": 5,          // 最大并发任务数
    "Timeout": 10000,            // 页面加载超时 (毫秒)
    "RetryTimes": 2,             // 默认重试次数
    "RetryDelay": 1000,          // 重试延迟 (毫秒)
    "Headless": true,            // 无头模式（建议开启）
    "BrowserPoolSize": 3         // 浏览器连接池大小
  }
}
```

### rules.json

```json
{
  "rules": [
    {
      "id": "bom-ai-price",
      "name": "BOM.ai 价格查询",
      "websiteUrl": "https://www.bom.ai/components-storage/{modelNumber}.html",
      "resultSelectors": {
        "price": "#bomID_quotePrice_01"
      },
      "waitTime": 500,
      "timeout": 10000,
      "enabled": true
    }
  ]
}
```

**配置热重载**: 修改 `rules.json` 后自动生效，无需重启程序。

## 📊 性能指标

| 指标 | 数值 |
|------|------|
| 单次查询响应时间 | 2-3 秒 |
| 批量查询响应时间 | 2-5 秒 |
| 最大并发任务数 | 5 个 |
| 浏览器池大小 | 3 个实例 |
| 缓存有效期 | 7 天 |
| 性能优化提升 | 60-70% |

## 📁 项目结构

```
WGetCrawler/
├── WGetCrawler.Api/           # 主程序
│   ├── Controllers/           # API 控制器
│   │   ├── CrawlController.cs      # 爬取任务控制器
│   │   ├── TasksController.cs      # 任务查询控制器
│   │   ├── RulesController.cs      # 规则管理控制器
│   │   └── HealthController.cs     # 健康检查控制器
│   ├── Services/              # 业务服务
│   │   ├── CrawlerService.cs       # 爬虫核心服务
│   │   ├── TaskSchedulerService.cs # 任务调度服务
│   │   ├── RuleConfigService.cs    # 规则配置服务
│   │   └── CacheService.cs         # 缓存服务
│   ├── Models/                # 数据模型
│   │   ├── CrawlRule.cs            # 爬虫规则模型
│   │   └── CrawlTask.cs            # 爬取任务模型
│   ├── Config/                # 配置类
│   │   └── AppSettings.cs          # 配置选项
│   ├── Middleware/            # 中间件
│   │   └── ExceptionHandlerMiddleware.cs
│   ├── Program.cs             # 程序入口
│   ├── appsettings.json       # 应用配置
│   └── rules.json             # 爬虫规则
├── frontend/                  # 前端项目（可选）
├── 打包发布.ps1               # 打包脚本
└── README.md                  # 本文档
```

## 🔧 技术栈

- **.NET 8.0** - 现代化的跨平台框架
- **ASP.NET Core** - Web API 框架
- **PuppeteerSharp 20.2.4** - 浏览器自动化
- **Serilog** - 结构化日志
- **IMemoryCache** - 内存缓存
- **Channel** - 异步任务队列
- **AspNetCoreRateLimit** - API 速率限制
- **Swagger/OpenAPI** - API 文档

## 🛡️ 可靠性保障

### 错误处理
- ✅ 自动重试机制（默认3次）
- ✅ 超时控制（避免无限等待）
- ✅ 异常捕获和友好错误提示
- ✅ 详细的日志记录

### 资源管理
- ✅ 浏览器连接池（复用实例）
- ✅ 任务队列（避免资源耗尽）
- ✅ 内存缓存（减少重复查询）
- ✅ 自动清理过期数据

### 数据准确性
- ✅ 直接访问详情页（避免搜索歧义）
- ✅ 精确的 CSS 选择器
- ✅ 数据验证和清洗
- ✅ 重试确保成功率

## ❓ 常见问题

### 1. 程序启动失败？

**问题**: 端口 3000 被占用

**解决**: 修改 `appsettings.json` 中的 `Application.Port` 为其他端口（如 5000）

---

**问题**: 首次启动很慢

**原因**: 正在下载 Chromium 浏览器（约 150MB）

**解决**: 耐心等待下载完成，后续启动会很快

---

### 2. 查询失败或超时？

**可能原因**:
- 网络连接问题
- 型号不存在或拼写错误
- 网站临时不可用

**解决方法**:
1. 检查网络连接
2. 确认型号正确
3. 查看日志文件（控制台输出）
4. 增加重试次数或超时时间

---

### 3. 如何批量查询？

在 `models` 数组中添加多个型号：

```json
{
  "models": [
    "STM32F103C8T6",
    "STM32F407VET6",
    "ESP32-WROOM-32"
  ],
  "useCache": true,
  "retryTimes": 3
}
```

---

### 4. 如何远程访问？

**步骤**:

1. 找到本机 IP 地址
   ```powershell
   ipconfig | findstr IPv4
   ```

2. 修改 `appsettings.json`（如需）
   ```json
   {
     "Application": {
       "Port": 3000
     }
   }
   ```

3. 配置防火墙允许 3000 端口

4. 使用 IP 访问
   ```
   http://192.168.1.100:3000
   ```

---

### 5. 如何查看日志？

日志会输出到：
- 控制台（实时）
- `logs/` 目录（文件）

---

### 6. 支持其他网站吗？

可以！修改 `rules.json` 添加新规则：

```json
{
  "rules": [
    {
      "id": "your-website-rule",
      "name": "你的网站",
      "websiteUrl": "https://example.com/search?q={modelNumber}",
      "resultSelectors": {
        "price": ".price-selector"
      },
      "enabled": true
    }
  ]
}
```

## 📖 相关文档

- **API使用示例.md** - 详细的编程示例
- **快速开始.txt** - 新手快速入门
- **VERSION.txt** - 版本信息和更新日志
- **API参数修正说明.md** - API参数变更说明

## 🔄 版本历史

### v1.0.0-rev2 (2025-11-13)
- ✅ 修复 API 路由问题（查询任务状态）
- ✅ 修复 PuppeteerSharp 导航异常
- ✅ 优化错误处理和异常回退
- ✅ 更新所有文档和示例

### v1.0.0 (2025-11-13)
- ✅ 初始版本发布
- ✅ 实现核心价格查询功能
- ✅ 性能优化 60-70%
- ✅ 单文件发布支持

## 📜 许可证

MIT License

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📞 支持

如有问题，请：
1. 查看本 README 文档
2. 查看日志文件排查问题
3. 访问 `/swagger` 查看 API 文档
4. 提交 Issue

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给个 Star！**

Made with ❤️ by WGetCrawler Team

</div>
