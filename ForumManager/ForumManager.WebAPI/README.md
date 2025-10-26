# 论坛服务 API 文档

## 概述

论坛服务提供完整的论坛功能，包括帖子发布、评论、点赞、收藏等核心功能。

## 技术栈

- **后端框架**: ASP.NET Core 8.0
- **数据库**: PostgreSQL + EF Core
- **缓存**: Redis
- **认证**: JWT Bearer Token
- **架构模式**: DDD (领域驱动设计) + CQRS

## 功能特性

### 帖子管理
- ✅ 发布帖子
- ✅ 编辑帖子
- ✅ 删除帖子
- ✅ 查看帖子列表（支持分页、分类筛选、关键词搜索）
- ✅ 查看帖子详情
- ✅ 获取热门帖子

### 评论功能
- ✅ 添加评论
- ✅ 编辑评论
- ✅ 删除评论
- ✅ 回复评论（支持嵌套回复）
- ✅ 查看评论列表

### 互动功能
- ✅ 点赞帖子/评论
- ✅ 取消点赞
- ✅ 收藏帖子
- ✅ 取消收藏

### 分类系统
- 📚 找学习搭子
- 🏃 运动组队
- 💻 技术讨论
- 💭 生活分享
- 💼 求职招聘
- 📝 其他

## API 端点

### 帖子相关

#### 获取帖子列表
```
GET /api/posts
```
**查询参数:**
- `pageIndex` (int): 页码，从0开始
- `pageSize` (int): 每页数量，最大100
- `category` (PostCategory): 帖子分类（可选）
- `keyword` (string): 搜索关键词（可选）

#### 获取热门帖子
```
GET /api/posts/hot
```
**查询参数:**
- `count` (int): 返回数量，最大50

#### 获取帖子详情
```
GET /api/posts/{postId}
```

#### 创建帖子
```
POST /api/posts
Authorization: Bearer {token}
```
**请求体:**
```json
{
  "title": "帖子标题",
  "content": "帖子内容",
  "category": 1,
  "tags": ["标签1", "标签2"]
}
```

#### 编辑帖子
```
PUT /api/posts/{postId}
Authorization: Bearer {token}
```

#### 删除帖子
```
DELETE /api/posts/{postId}
Authorization: Bearer {token}
```

#### 点赞帖子
```
POST /api/posts/{postId}/like
Authorization: Bearer {token}
```

#### 收藏帖子
```
POST /api/posts/{postId}/favorite
Authorization: Bearer {token}
```

### 评论相关

#### 获取帖子评论
```
GET /api/comments/post/{postId}
```

#### 获取评论详情
```
GET /api/comments/{commentId}
```

#### 添加评论
```
POST /api/comments
Authorization: Bearer {token}
```
**请求体:**
```json
{
  "postId": "帖子ID",
  "content": "评论内容",
  "parentCommentId": "父评论ID（可选）"
}
```

#### 编辑评论
```
PUT /api/comments/{commentId}
Authorization: Bearer {token}
```

#### 删除评论
```
DELETE /api/comments/{commentId}
Authorization: Bearer {token}
```

#### 点赞评论
```
POST /api/comments/{commentId}/like
Authorization: Bearer {token}
```

## 数据模型

### PostCategory 枚举
```csharp
public enum PostCategory
{
    StudyPartner = 1,    // 找学习搭子
    SportsTeam = 2,      // 运动组队
    TechDiscussion = 3,  // 技术讨论
    LifeSharing = 4,     // 生活分享
    JobHunting = 5,      // 求职招聘
    Other = 99           // 其他
}
```

### PostStatus 枚举
```csharp
public enum PostStatus
{
    Draft = 1,      // 草稿
    Published = 2,  // 已发布
    Deleted = 3,    // 已删除
    Hidden = 4      // 已隐藏
}
```

## 性能优化

### Redis 缓存策略
- **帖子详情**: 5分钟缓存
- **热门帖子**: 10分钟缓存
- **用户权限**: 根据JWT过期时间缓存

### 数据库优化
- 合理的索引设计
- 分页查询优化
- 热门帖子算法优化

## 领域事件

### PostCreatedEvent
当帖子创建时触发，可用于：
- 发送通知
- 更新搜索索引
- 统计用户活跃度

### CommentAddedEvent
当评论添加时触发，可用于：
- 通知帖子作者
- 通知被回复的用户
- 更新帖子热度

## 部署说明

### 环境要求
- .NET 8.0 Runtime
- PostgreSQL 12+
- Redis 6+

### 数据库配置
```json
{
  "ConnectionStrings": {
    "ForumConnection": "Host=localhost;Database=ForumDB;Username=postgres;Password=123456",
    "Redis": "localhost:6379"
  }
}
```

### JWT 配置
```json
{
  "Jwt": {
    "SecretKey": "YourSecretKey",
    "Issuer": "ForumManagerAPI",
    "Audience": "ForumManagerClient",
    "ExpiryInMinutes": 60
  }
}
```

## 开发指南

### 运行项目
```bash
cd ForumManager.WebAPI
dotnet run
```

### 数据库迁移
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 测试 API
访问 `https://localhost:7001/swagger` 查看 Swagger UI
