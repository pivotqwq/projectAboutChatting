# ChatService API 文档

## 概述

ChatService 提供实时聊天功能 API，包括私聊、群聊、消息管理和在线状态等功能。

**Base URL**: `http://localhost:9293`

**认证方式**: JWT Bearer Token

## ⚠️ 重要说明 - 认证流程

**ChatService 不提供用户登录和注册功能！**

用户认证由 **UserManager 服务**统一处理。正确的使用流程：

### 步骤 1: 在 UserManager 登录获取 Token

```bash
# UserManager 服务地址：http://localhost:9291
POST http://localhost:9291/api/Login/LoginByPhoneAndPassword
Content-Type: application/json

{
  "userBasic": {
    "phoneNumber": "13800138000",
    "email": "user@example.com"
  },
  "password": "yourpassword"
}
```

**响应**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "用户名"
}
```

### 步骤 2: 使用 Token 访问 ChatService

- **HTTP API**: 在请求头添加 `Authorization: Bearer {accessToken}`
- **SignalR WebSocket**: 连接时添加查询参数 `?access_token={accessToken}`

> 📖 详细的认证接口文档请参考 UserManager 服务的 `API接口文档（用户管理）.md`

---

## 消息管理 (Messages API)

### 获取私聊历史消息

```http
GET /api/messages/private/{userId}?beforeUtc=2024-01-15T10:30:00Z&pageSize=50
Authorization: Bearer {token}
```

**路径参数**:
- `userId` (必需): 对方用户ID

**查询参数**:
- `beforeUtc` (可选): 获取此时间之前的消息，格式为 ISO 8601 UTC 时间
- `pageSize` (可选): 每页数量，默认 50，最大 200

**响应示例**:
```json
[
  {
    "Id": "abc123def456",
    "Type": "private",
    "FromUserId": "user123",
    "ToUserId": "user456",
    "GroupId": null,
    "Content": "Hello!",
    "CreatedAt": "2024-01-15T10:30:00Z"
  },
  {
    "Id": "xyz789uvw012",
    "Type": "private",
    "FromUserId": "user456",
    "ToUserId": "user123",
    "GroupId": null,
    "Content": "Hi there!",
    "CreatedAt": "2024-01-15T10:31:00Z"
  }
]
```

**说明**: 
- 返回的消息按时间升序排列（最早的在前）
- 会同时返回发送和接收的消息

---

### 获取群聊历史消息

```http
GET /api/messages/group/{groupId}?beforeUtc=2024-01-15T10:30:00Z&pageSize=50
Authorization: Bearer {token}
```

**路径参数**:
- `groupId` (必需): 群组ID

**查询参数**:
- `beforeUtc` (可选): 获取此时间之前的消息，格式为 ISO 8601 UTC 时间
- `pageSize` (可选): 每页数量，默认 50，最大 200

**响应示例**:
```json
[
  {
    "Id": "xyz789",
    "Type": "group",
    "FromUserId": "user123",
    "ToUserId": null,
    "GroupId": "group001",
    "Content": "Hello everyone!",
    "CreatedAt": "2024-01-15T10:30:00Z"
  }
]
```

---

### 获取频道历史消息（公屏聊天）

```http
GET /api/messages/channel/{channelId}?beforeUtc=2024-01-15T10:30:00Z&pageSize=50
Authorization: Bearer {token}
```

**路径参数**:
- `channelId` (必需): 频道ID

**查询参数**:
- `beforeUtc` (可选): 获取此时间之前的消息，格式为 ISO 8601 UTC 时间
- `pageSize` (可选): 每页数量，默认 50，最大 200

**响应示例**:
```json
[
  {
    "Id": "abc456",
    "Type": "channel",
    "FromUserId": "user123",
    "ToUserId": null,
    "GroupId": "channel-001",
    "Content": "大家好！",
    "CreatedAt": "2024-01-15T10:30:00Z"
  }
]
```

**说明**: 
- 频道消息为公屏随机频道聊天，无需好友关系
- 消息按时间升序排列

---

## 语音消息 API

### 上传语音文件

```http
POST /api/voice/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

**请求参数（Form Data）**:
- `file` (File, 必需): 语音文件（支持 wav, mp3, m4a, ogg, webm 格式）
- `recognize` (boolean, 可选): 是否自动进行语音识别，默认 false
- `language` (string, 可选): 识别语言代码，默认 "zh-CN"

**响应示例**:
```json
{
  "filePath": "voices/20241106120000_abc123def456.webm",
  "fileUrl": "http://localhost:9293/voices/20241106120000_abc123def456.webm",
  "fileName": "recording.webm",
  "fileSize": 123456,
  "uploadedAt": "2024-11-06T12:00:00Z",
  "recognizedText": "识别出的文字内容",
  "recognitionStatus": "success"
}
```

**说明**:
- 文件大小限制：10MB
- 支持的格式：`.wav`, `.mp3`, `.m4a`, `.ogg`, `.webm`
- 如果 `recognize=true`，服务器会自动进行语音识别

---

### 获取语音文件信息

```http
GET /api/voice/info?filePath=voices/20241106120000_abc123def456.webm
Authorization: Bearer {token}
```

**查询参数**:
- `filePath` (string, 必需): 语音文件的相对路径

**响应示例**:
```json
{
  "filePath": "voices/20241106120000_abc123def456.webm",
  "fileUrl": "http://localhost:9293/voices/20241106120000_abc123def456.webm",
  "fileName": "20241106120000_abc123def456.webm",
  "fileSize": 123456,
  "extension": ".webm",
  "createdTime": "2024-11-06T12:00:00Z",
  "lastModified": "2024-11-06T12:00:00Z"
}
```

---

### 下载/播放语音文件

```http
GET /api/voice/download?filePath=voices/20241106120000_abc123def456.webm
Authorization: Bearer {token}
```

**查询参数**:
- `filePath` (string, 必需): 语音文件的相对路径

**响应**:
- 返回语音文件的二进制流
- Content-Type 根据文件格式自动设置（audio/webm, audio/ogg 等）

**说明**:
- 可以直接在 HTML `<audio>` 标签中使用返回的 URL 播放语音
- 也可以直接访问静态文件：`http://localhost:9293/voices/{文件名}`（如果配置了静态文件服务）

---

### 语音识别（已暂时屏蔽）

```http
POST /api/voice/recognize
Authorization: Bearer {token}
Content-Type: application/x-www-form-urlencoded
```

**请求参数（Form Data）**:
- `filePath` (string, 必需): 语音文件的相对路径
- `language` (string, 可选): 识别语言代码，默认 "zh-CN"

**响应示例**:
```json
{
  "filePath": "voices/20241106120000_abc123def456.webm",
  "recognizedText": "识别出的文字内容",
  "recognitionStatus": "success",
  "language": "zh-CN"
}
```

---

### 删除语音文件

```http
DELETE /api/voice/{filePath}
Authorization: Bearer {token}
```

**路径参数**:
- `filePath` (必需): 语音文件的相对路径（需要 URL 编码）

**响应示例**:
```json
{
  "message": "文件已删除",
  "filePath": "voices/20241106120000_abc123def456.webm"
}
```

---

## 频道管理 API

### 获取随机分配的频道

```http
GET /api/channels/assign-random
Authorization: Bearer {token}
```

**描述**: 为当前用户随机分配一个频道ID（相同用户ID总是分配到相同频道）

**成功响应** (200 OK):
```json
{
  "channelId": "channel-005"
}
```

---

### 获取所有可用频道列表

```http
GET /api/channels/list
Authorization: Bearer {token}
```

**描述**: 获取所有可用的频道ID列表

**成功响应** (200 OK):
```json
[
  "channel-001",
  "channel-002",
  "channel-003",
  ...
]
```

---

## 在线状态 (Presence API)

### 获取所有在线用户

```http
GET /api/presence/online-users
```

**说明**: 此接口允许匿名访问（不需要认证）

**响应示例**:
```json
[
  "user123",
  "user456",
  "user789"
]
```

---

### 检查用户是否在线

```http
GET /api/presence/is-online/{userId}
Authorization: Bearer {token}
```

**路径参数**:
- `userId` (必需): 要检查的用户ID

**响应示例**:
```json
{
  "userId": "user123",
  "online": true
}
```

---

## SignalR 实时通信

### 连接端点

```
ws://localhost:9293/hubs/chat?access_token={JWT_TOKEN}
```

**重要**: 必须在查询字符串中传递 `access_token` 参数进行认证。

### 客户端可调用的方法

#### 1. 发送私聊消息

```javascript
await connection.invoke("SendPrivateMessage", toUserId, content);
```

**参数**:
- `toUserId` (string): 接收者用户ID
- `content` (string): 消息内容

**服务端触发的事件**:
- 对方会收到 `ReceivePrivateMessage` 事件
- 自己会收到 `PrivateMessageSent` 事件（发送确认）

---

#### 2. 发送群聊消息

```javascript
await connection.invoke("SendGroupMessage", groupId, content);
```

**参数**:
- `groupId` (string): 群组ID
- `content` (string): 消息内容

**服务端触发的事件**:
- 群组所有成员会收到 `ReceiveGroupMessage` 事件

---

#### 3. 加入群组

```javascript
await connection.invoke("JoinGroup", groupId);
```

**参数**:
- `groupId` (string): 群组ID

**说明**: 加入群组后才能接收该群组的消息

---

#### 4. 离开群组

```javascript
await connection.invoke("LeaveGroup", groupId);
```

**参数**:
- `groupId` (string): 群组ID

**说明**: 离开群组后将不再接收该群组的消息

---

#### 5. 加入频道（公屏聊天）

```javascript
await connection.invoke("JoinChannel", channelId);
```

**参数**:
- `channelId` (string): 频道ID

**说明**: 加入频道后才能接收该频道的消息（公屏聊天，无需好友关系）

---

#### 6. 离开频道

```javascript
await connection.invoke("LeaveChannel", channelId);
```

**参数**:
- `channelId` (string): 频道ID

**说明**: 离开频道后将不再接收该频道的消息

---

#### 7. 发送频道消息（公屏聊天）

```javascript
await connection.invoke("SendChannelMessage", channelId, content);
```

**参数**:
- `channelId` (string): 频道ID
- `content` (string): 消息内容

**服务端触发的事件**:
- 频道所有成员会收到 `ReceiveChannelMessage` 事件

---

#### 8. 发送私聊语音消息

```javascript
await connection.invoke("SendPrivateVoiceMessage", toUserId, voiceFilePath, voiceFileUrl, duration, recognizedText);
```

**参数**:
- `toUserId` (string): 接收者用户ID
- `voiceFilePath` (string): 语音文件路径（从上传接口获取）
- `voiceFileUrl` (string, 可选): 语音文件访问URL
- `duration` (number, 可选): 语音时长（秒）
- `recognizedText` (string, 可选): 语音识别的文字

**说明**:
- 需要先调用 `/api/voice/upload` 上传语音文件
- 语音识别可通过上传接口自动完成，或单独调用 `/api/voice/recognize`

---

#### 9. 发送群聊语音消息

```javascript
await connection.invoke("SendGroupVoiceMessage", groupId, voiceFilePath, voiceFileUrl, duration, recognizedText);
```

**参数**:
- `groupId` (string): 群组ID
- `voiceFilePath` (string): 语音文件路径
- `voiceFileUrl` (string, 可选): 语音文件访问URL
- `duration` (number, 可选): 语音时长（秒）
- `recognizedText` (string, 可选): 语音识别的文字

**服务端触发的事件**:
- 群组所有成员会收到 `ReceiveGroupMessage` 事件（MessageType 为 "voice"）

---

#### 10. 发送频道语音消息（公屏聊天）

```javascript
await connection.invoke("SendChannelVoiceMessage", channelId, voiceFilePath, voiceFileUrl, duration, recognizedText);
```

**参数**:
- `channelId` (string): 频道ID
- `voiceFilePath` (string): 语音文件路径
- `voiceFileUrl` (string, 可选): 语音文件访问URL
- `duration` (number, 可选): 语音时长（秒）
- `recognizedText` (string, 可选): 语音识别的文字

**服务端触发的事件**:
- 频道所有成员会收到 `ReceiveChannelMessage` 事件（MessageType 为 "voice"）

---

### 服务端推送的事件

#### 接收私聊消息

```javascript
connection.on("ReceivePrivateMessage", (message) => {
  console.log("收到私聊消息:", message);
  // message 格式:
  // {
  //   Id: "abc123",
  //   Type: "private",
  //   FromUserId: "user123",
  //   ToUserId: "user456",
  //   GroupId: null,
  //   Content: "Hello!",
  //   CreatedAt: "2024-01-15T10:30:00Z"
  // }
});
```

---

#### 私聊消息发送确认

```javascript
connection.on("PrivateMessageSent", (message) => {
  console.log("私聊消息已发送:", message);
});
```

---

#### 接收群聊消息

```javascript
connection.on("ReceiveGroupMessage", (message) => {
  console.log("收到群聊消息:", message);
  // message 格式:
  // {
  //   Id: "xyz789",
  //   Type: "group",
  //   FromUserId: "user123",
  //   ToUserId: null,
  //   GroupId: "group001",
  //   Content: "Hello everyone!",
  //   CreatedAt: "2024-01-15T10:30:00Z"
  // }
});
```

---

#### 接收频道消息（公屏聊天）

```javascript
connection.on("ReceiveChannelMessage", (message) => {
  console.log("收到频道消息:", message);
  // message 格式:
  // {
  //   Id: "abc456",
  //   Type: "channel",
  //   FromUserId: "user123",
  //   ToUserId: null,
  //   GroupId: "channel-001",
  //   Content: "大家好！",
  //   CreatedAt: "2024-01-15T10:30:00Z",
  //   MessageType: "text"  // 文本消息为 "text"，语音消息为 "voice"
  // }
});
```

**语音消息格式**:
当 `MessageType === "voice"` 时，消息还包含以下字段：
```javascript
{
  // ... 基本字段同上
  MessageType: "voice",
  VoiceFilePath: "voices/20241106120000_abc123def456.webm",
  VoiceFileUrl: "http://localhost:9293/voices/20241106120000_abc123def456.webm",
  VoiceDuration: 10,  // 语音时长（秒）
  RecognizedText: "识别出的文字内容"  // 如果有语音识别
}
```

---

### cURL 示例

```bash
# 1. 在 UserManager 登录
curl -X POST http://localhost:9291/api/Login/LoginByPhoneAndPassword \
  -H "Content-Type: application/json" \
  -d '{
    "userBasic": {
      "phoneNumber": "13800138000"
    },
    "password": "yourpassword"
  }'

# 响应示例:
# {
#   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "userId": "...",
#   "userName": "..."
# }

# 2. 使用 token 获取私聊历史
curl -X GET "http://localhost:9293/api/messages/private/user456?pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 3. 获取群聊历史
curl -X GET "http://localhost:9293/api/messages/group/group001?pageSize=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 4. 获取随机分配的频道
curl -X GET "http://localhost:9293/api/channels/assign-random" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 5. 获取频道历史消息
curl -X GET "http://localhost:9293/api/messages/channel/channel-001?pageSize=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 6. 获取在线用户（无需认证）
curl -X GET http://localhost:9293/api/presence/online-users

# 7. 检查用户是否在线
curl -X GET http://localhost:9293/api/presence/is-online/user123 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 错误处理

### 常见错误

| HTTP 状态码 | 描述 | 解决方案 |
|------------|------|---------|
| 401 | 未授权 - Token 无效或已过期 | 重新在 UserManager 登录获取新 token |
| 403 | 禁止访问 - 权限不足 | 检查用户权限 |
| 404 | 资源不存在 | 检查 userId 或 groupId 是否正确 |
| 500 | 服务器内部错误 | 查看服务器日志 |

### SignalR 连接错误

如果 SignalR 连接失败，检查：
1. Token 是否正确传递在查询字符串中
2. Token 是否有效（未过期）
3. JWT 配置是否与 UserManager 一致

---

## 数据模型

### 消息 (ChatMessage)

```typescript
interface ChatMessage {
  Id: string;                    // 消息唯一ID
  Type: 'private' | 'group' | 'channel';  // 消息类型
  FromUserId: string;            // 发送者用户ID
  ToUserId: string | null;       // 接收者用户ID（私聊时使用）
  GroupId: string | null;        // 群组ID（群聊时使用）或频道ID（频道消息时使用）
  Content: string;               // 消息内容（文本消息为文本内容，语音消息为识别文字）
  CreatedAt: string;             // 创建时间（ISO 8601 UTC）
  MessageType?: 'text' | 'voice'; // 消息类型：文本或语音（可选，默认为 "text"）
  VoiceFilePath?: string;         // 语音文件路径（语音消息时使用）
  VoiceFileUrl?: string;          // 语音文件访问URL（语音消息时使用）
  VoiceDuration?: number;         // 语音时长（秒，语音消息时使用）
  RecognizedText?: string;        // 语音识别的文字（语音消息时使用）
}
```

---

## 部署配置

### 端口配置

- **ChatService**: 9293
- **UserManager**: 5261（开发环境默认）

### JWT 配置要求

ChatService 的 JWT 配置必须与 UserManager 保持一致：

```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration123456",
    "Issuer": "UserManagerAPI",
    "Audience": "UserManagerClient"
  }
}
```

⚠️ **重要**: `SecretKey`、`Issuer` 和 `Audience` 必须与 UserManager 完全相同，否则 token 验证会失败。

---

## 更新日志

### v1.4.0 (2024-11-06)
- 新增语音消息功能
- 新增语音文件上传接口：`POST /api/voice/upload`
- 新增语音识别接口：`POST /api/voice/recognize`
- 新增语音文件删除接口：`DELETE /api/voice/{filePath}`
- SignalR 新增语音消息方法：`SendPrivateVoiceMessage`、`SendGroupVoiceMessage`、`SendChannelVoiceMessage`
- 消息模型扩展支持语音消息类型
- 集成 Azure Speech Service 进行语音识别

### v1.3.0 (2025-10-31)
- 新增公屏随机频道聊天功能
- 新增频道消息类型：`SendChannelMessage`、`JoinChannel`、`LeaveChannel`
- 新增频道历史查询接口：`GET /api/messages/channel/{channelId}`
- 新增频道管理接口：`GET /api/channels/assign-random`、`GET /api/channels/list`
- 支持三种聊天模式：私聊（好友）、群聊（群组）、公屏（随机频道）

### v1.2.0 (2025-10-31)
- 新增群聊成员校验：`SendGroupMessage`/`JoinGroup` 会调用 UserManager 校验成员资格
- 文档修正端口与 Base URL（9293 / 9291）
- 新增配置说明 `UserManager:BaseUrl` 用于服务间调用

### v1.1.0 (2024-10-18)
- 移除了认证接口（登录/注册），统一由 UserManager 处理
- 更新文档，明确认证流程
- 修正 SignalR Hub 方法名称

### v1.0.0 (2024-01-15)
- 初始版本发布
- 基础聊天功能
- 在线状态管理
- SignalR 实时通信

---

## 技术支持

- **Swagger UI**: http://localhost:9293/swagger
- **MongoDB 数据库**: chatdb
- **Redis**: 用于在线状态管理

如有问题，请查看服务器日志或联系开发团队。

---

## 聊天机器人 API

### 获取聊天机器人信息

```http
GET /api/chatbot/info
Authorization: Bearer {token}
```

**描述**: 获取聊天机器人的用户ID和配置信息

**响应示例**:
```json
{
  "botUserId": "ai-chatbot",
  "name": "AI助手",
  "description": "智能聊天机器人，基于阿里云百炼千问模型",
  "enabled": true,
  "supportsVoice": false,
  "supportsImage": false
}
```

**说明**:
- `botUserId`: 聊天机器人的用户ID，前端需要将此ID添加到用户的好友列表中
- `supportsVoice`: 机器人是否支持语音消息（当前为 false，只支持文本）
- `supportsImage`: 机器人是否支持图片消息（当前为 false）

---

### 获取AI对话token使用情况

```http
GET /api/chatbot/usage
Authorization: Bearer {token}
```

**描述**: 获取当前用户今日的AI对话token使用情况和剩余额度

**响应示例**:
```json
{
  "used": 1250,
  "limit": 3000,
  "remaining": 1750,
  "resetTime": "2024-11-07T00:00:00Z"
}
```

**说明**:
- `used`: 今日已使用的token数量
- `limit`: 每日token限制（3000）
- `remaining`: 剩余可用token数量
- `resetTime`: 重置时间（UTC时间，每日0点重置）

**使用限制**:
- 每个用户每天限制使用3000 token
- 超过限制后，机器人将回复："抱歉，您今日的AI对话次数已达上限（3000 token/天），请明天再试。"
- 使用量在每日0点（UTC时间）自动重置

---

## 配置说明（服务间调用）

ChatService 会调用 UserManager 校验群成员身份，请在 ChatService 的配置中设置：

```json
{
  "UserManager": {
    "BaseUrl": "http://localhost:9291"
  }
}
```

说明：未配置时默认使用 `http://localhost:9291`。

---

## 语音功能配置说明

### 语音存储配置

```json
{
  "VoiceStorage": {
    "Path": ""  // 语音文件存储路径，为空则使用默认路径 wwwroot/voices
  }
}
```

### 语音识别配置

**Azure Speech Service 配置**:
```json
{
  "VoiceRecognition": {
    "Azure": {
      "SubscriptionKey": "your-azure-subscription-key",
      "Region": "eastasia"
    }
  }
}
```

**说明**:
- `SubscriptionKey`: Azure Speech Service 的订阅密钥（必需）
- `Region`: Azure 服务区域，默认为 "eastasia"
- 需要在 [Azure Portal](https://portal.azure.com) 创建 Speech Service 资源并获取密钥

**前端实现方案**: 请参考 `前端语音功能实现方案.md` 文档。

---

## AI聊天机器人配置说明

### 阿里云百炼千问配置

```json
{
  "AiChatBot": {
    "BotUserId": "ai-chatbot",
    "Aliyun": {
      "ApiKey": "your-aliyun-api-key",
      "Model": "qwen-turbo"
    },
    "SystemPrompt": "你是一个友好的AI助手，请用简洁、友好的方式回答用户的问题。",
    "Temperature": "0.7",
    "MaxTokens": "2000",
    "TopP": "0.8"
  }
}
```

**配置说明**:
- `BotUserId`: 聊天机器人的用户ID（默认为 "ai-chatbot"），需要在 UserManager 中创建对应的用户
- `Aliyun.ApiKey`: 阿里云百炼千问的 API Key（必需）
- `Aliyun.Model`: 使用的模型名称（默认 "qwen-turbo"，可选 "qwen-plus", "qwen-max" 等）
- `SystemPrompt`: 系统提示词，用于设定AI的角色和风格
- `Temperature`: 温度参数（0-1），控制回复的随机性，默认 0.7
- `MaxTokens`: 最大回复长度，默认 2000
- `TopP`: 核采样参数，默认 0.8

**获取 API Key**:
1. 访问 [阿里云百炼控制台](https://dashscope.console.aliyun.com/)
2. 创建API Key
3. 将API Key配置到 `appsettings.json` 中

**前端实现方案**: 请参考 `前端AI聊天机器人实现方案.md` 文档。
