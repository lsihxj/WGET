# API 快速使用指南

## 🎯 一步查询价格

### 步骤 1: 启动服务
双击 `启动程序.bat`，等待出现 "Application started" 提示

### 步骤 2: 调用 API

#### 使用 PowerShell（推荐）

```powershell
# 一键测试
.\测试API.ps1
```

或者手动执行：

```powershell
# 提交查询
$task = Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" `
    -ContentType "application/json" `
    -Body '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 等待3秒
Start-Sleep 3

# 获取结果
Invoke-RestMethod "http://localhost:3000/api/Tasks/$($task.data.taskId)"
```

#### 使用浏览器

1. 打开 Swagger 文档: http://localhost:3000/swagger
2. 找到 POST `/api/Crawl` 接口
3. 点击 "Try it out"
4. 填入请求数据：
```json
{
  "models": ["STM32F103C8T6"],
  "useCache": true,
  "retryTimes": 3
}
```
5. 点击 "Execute"
6. 复制返回的 taskId
7. 找到 GET `/api/Tasks/{taskId}` 接口
8. 输入 taskId，点击 "Execute"

## 📊 响应数据说明

### 成功响应示例

```json
{
  "success": true,
  "data": {
    "taskId": "xxx-xxx-xxx",
    "status": "completed",
    "results": [
      {
        "modelNumber": "STM32F103C8T6",
        "status": "success",
        "data": {
          "price": "4.35"    // 价格（单位：元）
        },
        "duration": 2156      // 查询耗时（毫秒）
      }
    ]
  }
}
```

### 提取价格数据

```powershell
# PowerShell
$price = $result.data.results[0].data.price
Write-Host "价格: ¥$price"
```

```python
# Python
price = result["data"]["results"][0]["data"]["price"]
print(f"价格: ¥{price}")
```

```javascript
// JavaScript
const price = result.data.results[0].data.price;
console.log(`价格: ¥${price}`);
```

## 🔄 批量查询

一次查询多个型号：

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

响应会包含所有型号的结果。

## ⏱️ 查询时间

- 单个型号：约 2-3 秒
- 多个型号：并发处理，总时间约 2-5 秒

建议等待 3-5 秒后再查询结果。

## 🌐 远程访问

如需在其他电脑访问此服务：

1. 找到本机 IP 地址：
```powershell
ipconfig | findstr IPv4
```

2. 使用 IP 地址访问：
```
http://[本机IP]:3000
```

3. 确保防火墙允许 3000 端口访问

## 📞 完整示例

### Python 完整示例

```python
import requests
import time

def query_price(model_number):
    """查询单个型号价格"""
    # 1. 提交任务
    response = requests.post(
        "http://localhost:3000/api/Crawl",
        json={
            "models": [model_number],
            "useCache": True,
            "retryTimes": 3
        }
    )
    task_id = response.json()["data"]["taskId"]
    
    # 2. 等待完成
    time.sleep(3)
    
    # 3. 获取结果
    result = requests.get(f"http://localhost:3000/api/Tasks/{task_id}")
    data = result.json()
    
    # 4. 提取价格
    if data["data"]["results"][0]["status"] == "success":
        price = data["data"]["results"][0]["data"]["price"]
        return f"¥{price}"
    else:
        return "查询失败"

# 使用
price = query_price("STM32F103C8T6")
print(f"STM32F103C8T6 价格: {price}")
```

### C# 完整示例

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        var baseUrl = "http://localhost:3000";
        
        // 1. 提交任务
        var request = new
        {
            models = new[] { "STM32F103C8T6" },
            useCache = true,
            retryTimes = 3
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        
        var response = await client.PostAsync($"{baseUrl}/api/Crawl", content);
        var taskResponse = await response.Content.ReadAsStringAsync();
        var taskData = JsonDocument.Parse(taskResponse);
        var taskId = taskData.RootElement.GetProperty("data").GetProperty("taskId").GetString();
        
        // 2. 等待3秒
        await Task.Delay(3000);
        
        // 3. 获取结果
        var result = await client.GetStringAsync($"{baseUrl}/api/Tasks/{taskId}");
        Console.WriteLine(result);
    }
}
```

## 🎨 其他工具

### Postman

1. 新建请求
2. Method: POST
3. URL: `http://localhost:3000/api/Crawl`
4. Headers: `Content-Type: application/json`
5. Body (raw):
```json
{
  "models": ["STM32F103C8T6"],
  "useCache": true,
  "retryTimes": 3
}
```

### curl

```bash
# 提交
curl -X POST http://localhost:3000/api/Crawl \
  -H "Content-Type: application/json" \
  -d '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 查询（替换 taskId）
curl http://localhost:3000/api/Tasks/{taskId}
```

---

**更多详情请查看 README.md**
