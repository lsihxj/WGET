# API 参数和路由修正说明 (v2)

## 发现的问题

### 1. API参数错误 (已修复)
在另一台电脑测试API时，出现400错误（Bad Request）

### 2. API路由错误 (新发现)
提交任务成功但查询状态时返回404错误：
```
Error checking status: 远程服务器返回错误: (404) 未找到。
```

### 3. PuppeteerSharp导航错误 (新发现)
日志显示导航异常：
```
PuppeteerSharp.NavigationException: Protocol error (Page.navigate): Invalid referrerPolicy
```

## 问题分析

### 问题1：参数名称不匹配
**文档中的错误参数**：
```json
{
  "ruleId": "bom-ai-price",
  "modelNumbers": ["STM32F103C8T6"]
}
```

**后端实际需要的参数**：
```json
{
  "models": ["STM32F103C8T6"],
  "useCache": true,
  "retryTimes": 3
}
```

### 问题2：API路由不匹配
**文档中的错误路由**：
- 提交任务：`POST /api/Crawl` ✅ 正确
- 查询状态：`GET /api/Crawl/{taskId}` ❌ **错误**

**实际的正确路由**：
- 提交任务：`POST /api/Crawl` ✅
- 查询状态：`GET /api/Tasks/{taskId}` ✅ **正确**

原因：后端有两个控制器
- `CrawlController` - 负责提交任务
- `TasksController` - 负责查询任务状态

### 问题3：NavigationOptions兼容性
PuppeteerSharp 20.2.4 在某些环境下使用 NavigationOptions 会报错。

**修复方案**：添加异常捕获，如果带选项的导航失败，回退到简单导航。

## 修正内容

### 1. 参数修正
已更新以下文件使用正确的参数：
- ✅ `WGetCrawler-Windows/测试API.ps1`
- ✅ `WGetCrawler-Windows/API使用示例.md`
- ✅ `WGetCrawler-Windows/README.md`
- ✅ `WGetCrawler-Windows/快速开始.txt`

### 2. 路由修正
已更新查询任务状态的API路由：
- ✅ 从 `/api/Crawl/{taskId}` 改为 `/api/Tasks/{taskId}`

### 3. 代码修正
已更新 `CrawlerService.cs`：
- ✅ 添加 NavigationException 异常捕获
- ✅ 失败时自动回退到简单导航方式
- ✅ 添加 Timeout 参数

## 正确的API调用方式

### 提交任务
```
POST http://localhost:3000/api/Crawl
```

### 查询任务状态
```
GET http://localhost:3000/api/Tasks/{taskId}
```

### PowerShell 完整示例
```powershell
# 提交任务
$task = Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" `
    -ContentType "application/json" `
    -Body '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 等待3秒
Start-Sleep 3

# 查询结果
$result = Invoke-RestMethod -Uri "http://localhost:3000/api/Tasks/$($task.data.taskId)"
$result | ConvertTo-Json -Depth 10
```

### cURL 完整示例
```bash
# 提交任务
curl -X POST http://localhost:3000/api/Crawl \
  -H "Content-Type: application/json" \
  -d '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 查询结果（替换 {taskId} 为上面返回的任务ID）
curl http://localhost:3000/api/Tasks/{taskId}
```

### Python 完整示例
```python
import requests
import time

# 提交任务
data = {
    "models": ["STM32F103C8T6"],
    "useCache": True,
    "retryTimes": 3
}
response = requests.post("http://localhost:3000/api/Crawl", json=data)
task_id = response.json()["data"]["taskId"]

# 等待3秒
time.sleep(3)

# 查询结果
result = requests.get(f"http://localhost:3000/api/Tasks/{task_id}")
print(result.json())
```

## 参数说明

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| models | string[] | ✅ 是 | - | 要查询的型号列表 |
| useCache | boolean | ❌ 否 | true | 是否使用缓存 |
| retryTimes | int | ❌ 否 | 3 | 失败后重试次数 |

## 批量查询示例

```json
{
  "models": [
    "STM32F103C8T6",
    "STM32F407VET6",
    "ESP32-WROOM-32",
    "STM32F746VGT6"
  ],
  "useCache": true,
  "retryTimes": 3
}
```

## 重新打包

已重新生成安装包：
- 📦 `WGetCrawler-v1.0.0-Windows-x64.zip` (43.64 MB)
- 📅 更新时间：2025-11-13 12:53
- 🔄 修正版本：v2 (修正API路由和导航错误)

## 测试验证

请在另一台电脑上：
1. 解压新的 zip 包
2. 双击运行 `启动程序.bat`
3. 等待服务启动成功
4. 运行 `测试API.ps1` 验证功能

应该能正常返回价格数据。

---

**更新日期**：2025-11-13  
**修正版本**：v1.0.0-rev2 (修正API路由和PuppeteerSharp导航错误)

## 主要变更总结

| 项目 | 原值 | 新值 |
|------|------|------|
| 请求参数 | `ruleId`, `modelNumbers` | `models`, `useCache`, `retryTimes` |
| 查询API | `GET /api/Crawl/{id}` | `GET /api/Tasks/{id}` |
| 导航方式 | 单一NavigationOptions | 带异常回退的双重导航 |
