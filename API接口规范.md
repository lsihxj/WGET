# WGetCrawler API 接口规范文档

**版本**: v1.0.0  
**基础 URL**: `http://localhost:3000`  
**日期**: 2025-11-14

---

## 📋 目录

- [通用说明](#通用说明)
- [接口列表](#接口列表)
  - [1. 健康检查](#1-健康检查)
  - [2. 提交查询任务](#2-提交查询任务)
  - [3. 查询任务状态](#3-查询任务状态)
  - [4. 获取规则列表](#4-获取规则列表)
- [数据模型](#数据模型)
- [错误码说明](#错误码说明)
- [完整示例](#完整示例)

---

## 通用说明

### 请求头

```http
Content-Type: application/json
```

### 响应格式

所有接口都返回统一的 JSON 格式：

```json
{
  "code": 200,
  "message": "成功",
  "data": {}
}
```

### 状态码

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | 成功 |
| 400 | 请求参数错误 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

---

## 接口列表

### 1. 健康检查

**用途**: 检查服务是否正常运行

**请求**

```http
GET /health
```

**响应示例**

```json
{
  "code": 200,
  "message": "OK",
  "data": {
    "status": "ok",
    "rulesCount": 1,
    "timestamp": "2025-11-14T02:00:00Z"
  }
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| status | string | 服务状态（ok: 正常） |
| rulesCount | int | 已加载的规则数量 |
| timestamp | string | 当前时间（ISO 8601 格式） |

**测试命令**

```bash
# cURL
curl http://localhost:3000/health

# PowerShell
Invoke-RestMethod -Uri "http://localhost:3000/health"

# Python
import requests
response = requests.get("http://localhost:3000/health")
print(response.json())
```

---

### 2. 提交查询任务

**用途**: 提交一个或多个型号的价格查询任务

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

**请求参数**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| models | string[] | ✅ | - | 要查询的型号列表（不能为空） |
| useCache | boolean | ❌ | true | 是否使用缓存（缓存有效期 7 天） |
| retryTimes | int | ❌ | 3 | 失败重试次数（0-5） |

**响应示例**

```json
{
  "code": 200,
  "message": "任务已提交",
  "data": {
    "taskId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "status": "processing",
    "totalCount": 2
  }
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| taskId | string | 任务 ID（用于后续查询） |
| status | string | 任务状态（processing: 处理中） |
| totalCount | int | 总查询数量 |

**错误响应**

```json
{
  "code": 400,
  "message": "models 不能为空",
  "data": null
}
```

**测试命令**

```bash
# cURL
curl -X POST http://localhost:3000/api/Crawl \
  -H "Content-Type: application/json" \
  -d '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# PowerShell
$body = @{
    models = @("STM32F103C8T6")
    useCache = $true
    retryTimes = 3
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" `
    -ContentType "application/json" -Body $body

# Python
import requests
response = requests.post(
    "http://localhost:3000/api/Crawl",
    json={"models": ["STM32F103C8T6"], "useCache": True, "retryTimes": 3}
)
print(response.json())
```

---

### 3. 查询任务状态

**用途**: 根据任务 ID 查询任务状态和结果

**请求**

```http
GET /api/Tasks/{taskId}
```

**路径参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| taskId | string | ✅ | 任务 ID（提交任务时返回） |

**响应示例（处理中）**

```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "taskId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "status": "processing",
    "totalCount": 2,
    "completedCount": 1,
    "results": [
      {
        "modelNumber": "STM32F103C8T6",
        "status": "success",
        "data": {
          "price": "4.35"
        },
        "duration": 2156,
        "retryCount": 0
      }
    ]
  }
}
```

**响应示例（已完成）**

```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "taskId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
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
        "retryCount": 0,
        "errorMessage": null
      },
      {
        "modelNumber": "STM32F407VET6",
        "status": "success",
        "data": {
          "price": "12.80"
        },
        "duration": 2345,
        "retryCount": 0,
        "errorMessage": null
      }
    ]
  }
}
```

**响应示例（失败）**

```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "taskId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "status": "completed",
    "totalCount": 1,
    "completedCount": 1,
    "results": [
      {
        "modelNumber": "INVALID_MODEL",
        "status": "failed",
        "data": null,
        "duration": 10250,
        "retryCount": 3,
        "errorMessage": "未找到价格信息"
      }
    ]
  }
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| taskId | string | 任务 ID |
| status | string | 任务状态（processing: 处理中, completed: 已完成, failed: 失败） |
| totalCount | int | 总查询数量 |
| completedCount | int | 已完成数量 |
| results | array | 结果列表 |

**结果对象字段**

| 字段 | 类型 | 说明 |
|------|------|------|
| modelNumber | string | 型号 |
| status | string | 该型号的查询状态（success: 成功, failed: 失败） |
| data | object | 查询结果数据（包含 price 字段） |
| data.price | string | 价格（单位：元） |
| duration | int | 查询耗时（毫秒） |
| retryCount | int | 实际重试次数 |
| errorMessage | string | 错误信息（失败时） |

**错误响应（任务不存在）**

```json
{
  "code": 404,
  "message": "任务不存在: invalid-task-id",
  "data": null
}
```

**测试命令**

```bash
# cURL（替换 {taskId}）
curl http://localhost:3000/api/Tasks/a1b2c3d4-e5f6-7890-abcd-ef1234567890

# PowerShell
$taskId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
Invoke-RestMethod -Uri "http://localhost:3000/api/Tasks/$taskId"

# Python
import requests
task_id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
response = requests.get(f"http://localhost:3000/api/Tasks/{task_id}")
print(response.json())
```

---

### 4. 获取规则列表

**用途**: 获取已加载的爬虫规则列表

**请求**

```http
GET /api/Rules
```

**响应示例**

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

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| id | string | 规则 ID |
| name | string | 规则名称 |
| websiteUrl | string | 目标网站 URL 模板 |
| enabled | boolean | 是否启用 |

**测试命令**

```bash
# cURL
curl http://localhost:3000/api/Rules

# PowerShell
Invoke-RestMethod -Uri "http://localhost:3000/api/Rules"

# Python
import requests
response = requests.get("http://localhost:3000/api/Rules")
print(response.json())
```

---

## 数据模型

### CrawlRequest（提交任务请求）

```json
{
  "models": ["string"],
  "useCache": true,
  "retryTimes": 3
}
```

### TaskResponse（任务响应）

```json
{
  "taskId": "string",
  "status": "processing | completed | failed",
  "totalCount": 0,
  "completedCount": 0,
  "results": [
    {
      "modelNumber": "string",
      "status": "success | failed",
      "data": {
        "price": "string"
      },
      "duration": 0,
      "retryCount": 0,
      "errorMessage": "string | null"
    }
  ]
}
```

---

## 错误码说明

| Code | Message | 说明 | 解决方法 |
|------|---------|------|----------|
| 200 | 成功 | 请求成功 | - |
| 400 | models 不能为空 | 未提供型号列表 | 检查请求参数 |
| 400 | 未找到可用的爬虫规则 | rules.json 配置错误 | 检查配置文件 |
| 404 | 任务不存在 | taskId 无效或过期 | 重新提交任务 |
| 500 | 服务器内部错误 | 服务器异常 | 查看日志文件 |

---

## 完整示例

### 场景 1: 单个型号查询

#### PowerShell

```powershell
# 1. 提交任务
$task = Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" `
    -ContentType "application/json" `
    -Body '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

Write-Host "任务已提交，ID: $($task.data.taskId)"

# 2. 等待 3 秒
Start-Sleep -Seconds 3

# 3. 查询结果
$result = Invoke-RestMethod -Uri "http://localhost:3000/api/Tasks/$($task.data.taskId)"

# 4. 提取价格
if ($result.data.results[0].status -eq "success") {
    $price = $result.data.results[0].data.price
    Write-Host "STM32F103C8T6 价格: ¥$price"
} else {
    Write-Host "查询失败: $($result.data.results[0].errorMessage)"
}
```

#### Python

```python
import requests
import time

# 1. 提交任务
response = requests.post(
    "http://localhost:3000/api/Crawl",
    json={"models": ["STM32F103C8T6"], "useCache": True, "retryTimes": 3}
)
task_id = response.json()["data"]["taskId"]
print(f"任务已提交，ID: {task_id}")

# 2. 等待 3 秒
time.sleep(3)

# 3. 查询结果
result = requests.get(f"http://localhost:3000/api/Tasks/{task_id}")
data = result.json()

# 4. 提取价格
if data["data"]["results"][0]["status"] == "success":
    price = data["data"]["results"][0]["data"]["price"]
    print(f"STM32F103C8T6 价格: ¥{price}")
else:
    print(f"查询失败: {data['data']['results'][0]['errorMessage']}")
```

#### JavaScript

```javascript
const axios = require('axios');

async function queryPrice() {
  // 1. 提交任务
  const response = await axios.post('http://localhost:3000/api/Crawl', {
    models: ['STM32F103C8T6'],
    useCache: true,
    retryTimes: 3
  });
  
  const taskId = response.data.data.taskId;
  console.log(`任务已提交，ID: ${taskId}`);
  
  // 2. 等待 3 秒
  await new Promise(resolve => setTimeout(resolve, 3000));
  
  // 3. 查询结果
  const result = await axios.get(`http://localhost:3000/api/Tasks/${taskId}`);
  
  // 4. 提取价格
  if (result.data.data.results[0].status === 'success') {
    const price = result.data.data.results[0].data.price;
    console.log(`STM32F103C8T6 价格: ¥${price}`);
  } else {
    console.log(`查询失败: ${result.data.data.results[0].errorMessage}`);
  }
}

queryPrice();
```

#### C#

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:3000") };
        
        // 1. 提交任务
        var request = new { models = new[] { "STM32F103C8T6" }, useCache = true, retryTimes = 3 };
        var response = await client.PostAsJsonAsync("/api/Crawl", request);
        var taskData = await response.Content.ReadFromJsonAsync<dynamic>();
        var taskId = taskData.data.taskId.ToString();
        
        Console.WriteLine($"任务已提交，ID: {taskId}");
        
        // 2. 等待 3 秒
        await Task.Delay(3000);
        
        // 3. 查询结果
        var result = await client.GetFromJsonAsync<dynamic>($"/api/Tasks/{taskId}");
        
        // 4. 提取价格
        if (result.data.results[0].status.ToString() == "success")
        {
            var price = result.data.results[0].data.price.ToString();
            Console.WriteLine($"STM32F103C8T6 价格: ¥{price}");
        }
        else
        {
            Console.WriteLine($"查询失败: {result.data.results[0].errorMessage}");
        }
    }
}
```

---

### 场景 2: 批量查询

```powershell
# 1. 提交批量任务
$models = @(
    "STM32F103C8T6",
    "STM32F407VET6",
    "ESP32-WROOM-32",
    "STM32F746VGT6"
)

$task = Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" `
    -ContentType "application/json" `
    -Body (@{models=$models; useCache=$true; retryTimes=3} | ConvertTo-Json)

Write-Host "批量任务已提交，共 $($models.Count) 个型号"

# 2. 轮询直到完成
$maxWait = 30
$waited = 0

while ($waited -lt $maxWait) {
    Start-Sleep -Seconds 2
    $waited += 2
    
    $result = Invoke-RestMethod -Uri "http://localhost:3000/api/Tasks/$($task.data.taskId)"
    
    Write-Host "进度: $($result.data.completedCount)/$($result.data.totalCount) [$waited 秒]"
    
    if ($result.data.status -eq "completed") {
        break
    }
}

# 3. 显示所有结果
foreach ($item in $result.data.results) {
    if ($item.status -eq "success") {
        Write-Host "$($item.modelNumber): ¥$($item.data.price) ($($item.duration) ms)" -ForegroundColor Green
    } else {
        Write-Host "$($item.modelNumber): 失败 - $($item.errorMessage)" -ForegroundColor Red
    }
}
```

---

## 最佳实践

### 1. 轮询间隔

建议使用 2-3 秒的轮询间隔查询任务状态，避免过于频繁的请求。

### 2. 超时处理

设置合理的超时时间（建议 15-30 秒），避免无限等待。

### 3. 错误处理

始终检查 `code` 和 `status` 字段，妥善处理错误情况。

### 4. 批量优化

批量查询时，系统会并发处理，总时间约为 2-5 秒，而不是单个查询时间的累加。

### 5. 缓存利用

对于频繁查询的型号，建议启用缓存（`useCache: true`），可显著提升速度。

---

## 常见问题

### Q1: 任务提交后多久能完成？

A: 单个型号约 2-3 秒，多个型号并发处理约 2-5 秒。

### Q2: 任务结果保留多久？

A: 任务结果保留 30 分钟（可在配置文件中修改）。

### Q3: 可以同时提交多少个任务？

A: 建议不超过 5 个并发任务（受 `MaxConcurrent` 配置限制）。

### Q4: 如何判断任务已完成？

A: 检查 `status` 字段是否为 `"completed"` 或 `"failed"`。

### Q5: 价格单位是什么？

A: 价格单位为人民币（元），字符串格式，如 `"4.35"`。

---

## 版本更新

### v1.0.0 (2025-11-14)

- ✅ 初始版本
- ✅ 支持单个/批量型号查询
- ✅ 智能缓存机制
- ✅ 自动重试功能
- ✅ 完整的错误处理

---

**文档维护**: WGetCrawler Team  
**最后更新**: 2025-11-14  
**联系方式**: 请通过 GitHub Issues 反馈问题
