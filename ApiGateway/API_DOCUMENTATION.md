# API Gateway 接口文档

## 概述

API Gateway 作为微服务架构的统一入口，提供以下功能：
- 统一路由和负载均衡
- JWT 认证和授权
- 请求限流和熔断
- 数据聚合服务
- 请求日志和监控

## 基础信息

- **基础URL**: `http://localhost:5000`
- **认证方式**: JWT Bearer Token
- **响应格式**: JSON

## 认证

### 获取Token
通过 UserManager 服务获取 JWT Token：

```http
POST /api/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

### 使用Token
在请求头中添加认证信息：

```http
Authorization: Bearer <your-jwt-token>
```

## 聚合API端点

### 1. 获取用户资料及帖子列表

获取指定用户的详细资料和帖子列表。

```http
GET /api/aggregated/user/{userId}/profile-with-posts?page=1&pageSize=10
Authorization: Bearer <token>
```

**路径参数：**
- `userId` (string): 用户ID

**查询参数：**
- `page` (int, 可选): 页码，默认为1
- `pageSize` (int, 可选): 每页数量，默认为10

**响应示例：**
```json
{
  "user": {
    "id": "123",
    "username": "john_doe",
    "email": "john@example.com",
    "avatar": "https://example.com/avatar.jpg",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "posts": {
    "posts": [
      {
        "id": "456",
        "title": "Sample Post",
        "content": "Post content...",
        "createdAt": "2024-01-01T00:00:00Z",
        "likes": 10,
        "comments": 5
      }
    ],
    "totalCount": 25,
    "page": 1,
    "pageSize": 10
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 2. 获取帖子及评论

获取指定帖子的详细信息和评论列表。

```http
GET /api/aggregated/post/{postId}/with-comments?page=1&pageSize=10
Authorization: Bearer <token>
```

**路径参数：**
- `postId` (string): 帖子ID

**查询参数：**
- `page` (int, 可选): 页码，默认为1
- `pageSize` (int, 可选): 每页数量，默认为10

**响应示例：**
```json
{
  "post": {
    "id": "456",
    "title": "Sample Post",
    "content": "Post content...",
    "authorId": "123",
    "authorName": "john_doe",
    "createdAt": "2024-01-01T00:00:00Z",
    "likes": 10,
    "favorites": 3
  },
  "comments": {
    "comments": [
      {
        "id": "789",
        "content": "Great post!",
        "authorId": "124",
        "authorName": "jane_doe",
        "createdAt": "2024-01-01T01:00:00Z",
        "likes": 2
      }
    ],
    "totalCount": 15,
    "page": 1,
    "pageSize": 10
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 3. 获取用户仪表板数据

获取当前用户的仪表板数据，包括用户信息、最近帖子、收藏帖子和匹配统计。

```http
GET /api/aggregated/user/dashboard
Authorization: Bearer <token>
```

**响应示例：**
```json
{
  "user": {
    "id": "123",
    "username": "john_doe",
    "email": "john@example.com",
    "avatar": "https://example.com/avatar.jpg"
  },
  "recentPosts": {
    "posts": [
      {
        "id": "456",
        "title": "Recent Post 1",
        "createdAt": "2024-01-01T00:00:00Z",
        "likes": 5
      }
    ],
    "totalCount": 10
  },
  "favoritePosts": {
    "posts": [
      {
        "id": "789",
        "title": "Favorite Post 1",
        "createdAt": "2024-01-01T00:00:00Z",
        "likes": 15
      }
    ],
    "totalCount": 5
  },
  "matchingStats": {
    "totalMatches": 12,
    "activeMatches": 3,
    "likesReceived": 25,
    "likesSent": 18
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 4. 获取论坛统计信息

获取论坛的整体统计信息。

```http
GET /api/aggregated/forum/stats
Authorization: Bearer <token>
```

**响应示例：**
```json
{
  "totalPosts": 1250,
  "totalComments": 5600,
  "activeUsers": 150,
  "popularPosts": [
    {
      "id": "456",
      "title": "Most Popular Post",
      "likes": 100,
      "comments": 50,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## 代理API端点

API Gateway 将以下请求代理到相应的微服务：

### UserManager 服务
```http
GET /api/users/{userId}
POST /api/users/register
POST /api/users/login
PUT /api/users/{userId}
DELETE /api/users/{userId}
```

### ForumManager 服务
```http
GET /api/forum/posts
POST /api/forum/posts
GET /api/forum/posts/{postId}
PUT /api/forum/posts/{postId}
DELETE /api/forum/posts/{postId}
GET /api/forum/posts/{postId}/comments
POST /api/forum/posts/{postId}/comments
```

### ChatService 服务
```http
GET /api/chat/rooms
POST /api/chat/rooms
GET /api/chat/rooms/{roomId}/messages
POST /api/chat/rooms/{roomId}/messages
```

### MatchingService 服务
```http
GET /api/matching/profiles
POST /api/matching/profiles
GET /api/matching/matches
POST /api/matching/like
POST /api/matching/pass
```

## 健康检查

### 网关健康检查
```http
GET /api/aggregated/health
```

**响应示例：**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "service": "ApiGateway"
}
```

## 错误响应

### 认证错误
```json
{
  "error": "Unauthorized",
  "message": "Invalid or missing token",
  "statusCode": 401
}
```

### 限流错误
```json
{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded. Please try again later.",
  "statusCode": 429,
  "retryAfter": 60
}
```

### 服务错误
```json
{
  "error": "Service Unavailable",
  "message": "Downstream service is temporarily unavailable",
  "statusCode": 503
}
```

### 验证错误
```json
{
  "error": "Bad Request",
  "message": "Invalid request parameters",
  "statusCode": 400,
  "details": [
    {
      "field": "userId",
      "message": "User ID is required"
    }
  ]
}
```

## 限流信息

API Gateway 在响应头中包含限流信息：

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 85
X-RateLimit-Reset: 1640995200
```

## 请求ID

每个请求都会包含唯一的请求ID：

```http
X-Request-Id: a1b2c3d4
```

## 缓存策略

- **用户资料+帖子**: 缓存5分钟
- **帖子+评论**: 缓存3分钟
- **用户仪表板**: 缓存2分钟
- **论坛统计**: 缓存10分钟

## 示例代码

### JavaScript (Fetch)
```javascript
const token = 'your-jwt-token';

// 获取用户仪表板
const response = await fetch('/api/aggregated/user/dashboard', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const dashboard = await response.json();
console.log(dashboard);
```

### cURL
```bash
# 获取用户资料和帖子
curl -X GET \
  "http://localhost:5000/api/aggregated/user/123/profile-with-posts?page=1&pageSize=10" \
  -H "Authorization: Bearer your-jwt-token" \
  -H "Content-Type: application/json"
```

### C# (.NET)
```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await httpClient.GetAsync(
    "/api/aggregated/user/dashboard");
var dashboard = await response.Content.ReadAsStringAsync();
```

## 注意事项

1. **认证要求**: 除健康检查和公开端点外，所有API都需要有效的JWT Token
2. **限流策略**: 每个用户每分钟最多100个请求，突发请求限制为10个
3. **缓存机制**: 聚合数据会根据不同策略进行缓存，提高响应速度
4. **错误处理**: 所有错误都会返回标准化的错误响应格式
5. **日志记录**: 所有请求都会被记录，包括请求时间、响应时间和错误信息

## 版本信息

- **API版本**: v1
- **Gateway版本**: 1.0.0
- **支持的服务**: UserManager, ForumManager, ChatService, MatchingService
