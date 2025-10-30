# ChatService API 文档

## 概述

ChatService 提供实时聊天功能 API，包括私聊、群聊、消息管理和在线状态等功能。

**Base URL**: `http://localhost:9293

**认证方式**: JWT Bearer Token

## ⚠️ 重要说明 - 认证流程

**ChatService 不提供用户登录和注册功能！**

用户认证由 **UserManager 服务**统一处理。正确的使用流程：

### 步骤 1: 在 UserManager 登录获取 Token

```bash
# UserManager 服务地址: http://localhost:9291
POST http://localhost:5050/api/Login/LoginByPhoneAndPassword
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
ws://localhost:9391/hubs/chat?access_token={JWT_TOKEN}
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

### cURL 示例

```bash
# 1. 在 UserManager 登录
curl -X POST http://localhost:5050/api/Login/LoginByPhoneAndPassword \
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
curl -X GET "http://localhost:9391/api/messages/private/user456?pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 3. 获取群聊历史
curl -X GET "http://localhost:9391/api/messages/group/group001?pageSize=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 4. 获取在线用户（无需认证）
curl -X GET http://localhost:9391/api/presence/online-users

# 5. 检查用户是否在线
curl -X GET http://localhost:9391/api/presence/is-online/user123 \
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
  Type: 'private' | 'group';     // 消息类型
  FromUserId: string;            // 发送者用户ID
  ToUserId: string | null;       // 接收者用户ID（私聊时使用）
  GroupId: string | null;        // 群组ID（群聊时使用）
  Content: string;               // 消息内容
  CreatedAt: string;             // 创建时间（ISO 8601 UTC）
}
```

---

## 部署配置

### 端口配置

- **ChatService**: 9391
- **UserManager**: 5050（用于登录认证）

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
