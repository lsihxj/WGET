# API 快速参考卡片

> **快速查询**: 两步获取价格 → 提交任务 + 查询结果

---

## 🎯 基本信息

| 项目 | 值 |
|------|-----|
| **服务地址** | `http://localhost:3000` |
| **API 文档** | `http://localhost:3000/swagger` |
| **响应时间** | 2-3 秒（单个型号） |
| **缓存时间** | 7 天 |

---

## 📡 接口速查

### 1️⃣ 提交任务

```http
POST /api/Crawl
```

```json
{
  "models": ["STM32F103C8T6"],
  "useCache": true,
  "retryTimes": 3
}
```

**返回**: `taskId`

---

### 2️⃣ 查询结果

```http
GET /api/Tasks/{taskId}
```

**返回**: 包含价格的完整结果

---

### 3️⃣ 健康检查

```http
GET /health
```

---

### 4️⃣ 规则列表

```http
GET /api/Rules
```

---

## ⚡ 一键复制命令

### PowerShell（推荐）

```powershell
# 完整流程
$task = Invoke-RestMethod -Method Post -Uri "http://localhost:3000/api/Crawl" -ContentType "application/json" -Body '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'
Start-Sleep 3
Invoke-RestMethod "http://localhost:3000/api/Tasks/$($task.data.taskId)"
```

### cURL

```bash
# 步骤 1: 提交（返回 taskId）
curl -X POST http://localhost:3000/api/Crawl \
  -H "Content-Type: application/json" \
  -d '{"models":["STM32F103C8T6"],"useCache":true,"retryTimes":3}'

# 步骤 2: 查询（替换 {taskId}）
curl http://localhost:3000/api/Tasks/{taskId}
```

### Python

```python
import requests, time
r = requests.post("http://localhost:3000/api/Crawl", 
    json={"models":["STM32F103C8T6"],"useCache":True,"retryTimes":3})
time.sleep(3)
result = requests.get(f"http://localhost:3000/api/Tasks/{r.json()['data']['taskId']}")
print(result.json()["data"]["results"][0]["data"]["price"])
```

---

## 📊 返回数据结构

```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "taskId": "xxx-xxx",
    "status": "completed",
    "totalCount": 1,
    "completedCount": 1,
    "results": [
      {
        "modelNumber": "STM32F103C8T6",
        "status": "success",
        "data": {
          "price": "4.35"  ← 这是价格
        },
        "duration": 2156,
        "retryCount": 0
      }
    ]
  }
}
```

---

## 🔑 关键字段说明

| 字段 | 位置 | 说明 |
|------|------|------|
| `taskId` | 提交响应 | 任务 ID，用于查询 |
| `status` | 查询响应 | `processing` \| `completed` \| `failed` |
| `results[].status` | 查询响应 | `success` \| `failed` |
| `results[].data.price` | 查询响应 | **价格（元）** |
| `results[].duration` | 查询响应 | 耗时（毫秒） |

---

## ⚙️ 参数说明

### models（必填）

- 类型: `string[]`
- 示例: `["STM32F103C8T6", "STM32F407VET6"]`
- 说明: 要查询的型号列表

### useCache（可选）

- 类型: `boolean`
- 默认: `true`
- 说明: 是否使用缓存（7天有效）

### retryTimes（可选）

- 类型: `int`
- 默认: `3`
- 范围: `0-5`
- 说明: 失败重试次数

---

## ❓ 常见场景

### 场景 1: 单个型号查询

```json
{"models": ["STM32F103C8T6"], "useCache": true, "retryTimes": 3}
```

### 场景 2: 批量查询

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

### 场景 3: 强制刷新（不用缓存）

```json
{"models": ["STM32F103C8T6"], "useCache": false, "retryTimes": 3}
```

---

## ⚠️ 注意事项

### ✅ 正确做法

- 等待 2-3 秒后再查询结果
- 检查 `status` 字段判断任务状态
- 使用缓存提升查询速度
- 批量查询而不是多次单独查询

### ❌ 错误做法

- 提交后立即查询（可能还在处理中）
- 忽略错误状态
- 过于频繁地查询状态（建议间隔 2 秒）
- 对同一批型号重复提交多个任务

---

## 🔗 完整文档

- 📘 **API接口规范.md** - 完整接口文档
- 📗 **API使用示例.md** - 各语言详细示例
- 📙 **README.md** - 完整使用指南

---

## 🚀 快速测试

### 方法 1: 运行测试脚本

```powershell
.\测试API.ps1
```

### 方法 2: 运行验证脚本

```powershell
.\API测试验证.ps1
```

### 方法 3: 浏览器访问

```
http://localhost:3000/swagger
```

---

**更新日期**: 2025-11-14  
**版本**: v1.0.0
