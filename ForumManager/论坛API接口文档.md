# 论坛管理微服务 API 接口文档

## 概述

论坛管理微服务提供完整的论坛功能，包括帖子发布、评论、点赞、收藏等核心功能。

**基础URL**: `http://localhost:9292/api`  
**认证方式**: JWT Bearer Token  
**数据格式**: JSON  

## 认证说明

大部分API需要JWT认证，请在请求头中添加：
```
Authorization: Bearer {your_jwt_token}
```

## 帖子管理 API

### 1. 获取帖子列表

**接口**: `GET /api/posts`

**描述**: 获取分页的帖子列表，支持分类筛选和关键词搜索

**请求参数**:
| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | int | 否 | 0 | 页码，从0开始 |
| pageSize | int | 否 | 20 | 每页数量，最大100 |
| category | PostCategory | 否 | null | 帖子分类筛选 |
| keyword | string | 否 | null | 关键词搜索 |

**PostCategory 枚举值**:
- `1` - 找学习搭子
- `2` - 运动组队  
- `3` - 技术讨论
- `4` - 生活分享
- `5` - 求职招聘
- `99` - 其他

**响应示例**:
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "帖子标题",
      "content": "帖子内容",
      "titleImageBase64": null,
      "category": 1,
      "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": null,
      "viewCount": 100,
      "likeCount": 20,
      "commentCount": 15,
      "favoriteCount": 8,
      "tags": ["标签1", "标签2"]
    }
  ],
  "pageIndex": 0,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 2. 获取热门帖子

**接口**: `GET /api/posts/hot`

**描述**: 获取热门帖子列表，按热度排序

**请求参数**:
| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| count | int | 否 | 10 | 返回数量，最大50 |

**响应**: 返回 `PostResponse[]` 数组

### 3. 获取帖子详情

**接口**: `GET /api/posts/{postId}`

**描述**: 获取指定帖子的详细信息，包含评论列表

**路径参数**:
- `postId`: 帖子ID (Guid)

**响应示例**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "帖子标题",
  "content": "帖子内容",
  "titleImageBase64": null,
  "category": 1,
  "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": null,
  "viewCount": 101,
  "likeCount": 20,
  "commentCount": 15,
  "favoriteCount": 8,
  "tags": ["标签1", "标签2"],
  "comments": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "content": "评论内容",
      "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "parentCommentId": null,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": null,
      "likeCount": 5
    }
  ]
}
```

### 4. 创建帖子

**接口**: `POST /api/posts`

**描述**: 创建新帖子（需要认证）

**请求体**:
```json
{
  "title": "帖子标题",
  "content": "帖子内容",
  "titleImageBase64": "BASE64_STRING",
  "category": 1,
  "tags": ["标签1", "标签2"]
}
```

**验证规则**:
- `title`: 必填，最大200字符
- `content`: 必填，最大10000字符  
- `category`: 必填，PostCategory枚举值
- `tags`: 可选，字符串数组
 - `titleImageBase64`: 可选，图片Base64字符串（建议小于1MB）

**响应**: 返回创建的 `PostResponse`

### 5. 编辑帖子

**接口**: `PUT /api/posts/{postId}`

**描述**: 编辑指定帖子（需要认证，仅作者可编辑）

**路径参数**:
- `postId`: 帖子ID (Guid)

**请求体**:
```json
{
  "title": "新标题",
  "content": "新内容", 
  "titleImageBase64": "BASE64_STRING",
  "tags": ["新标签1", "新标签2"]
}
```

**响应**: 返回更新后的 `PostResponse`

### 6. 删除帖子

**接口**: `DELETE /api/posts/{postId}`

**描述**: 删除指定帖子（需要认证，仅作者可删除）

**路径参数**:
- `postId`: 帖子ID (Guid)

**响应**: `204 No Content`

### 7. 点赞/取消点赞帖子

**接口**: `POST /api/posts/{postId}/like`

**描述**: 切换帖子的点赞状态（需要认证）

**路径参数**:
- `postId`: 帖子ID (Guid)

**响应**: `200 OK`

### 8. 收藏/取消收藏帖子

**接口**: `POST /api/posts/{postId}/favorite`

**描述**: 切换帖子的收藏状态（需要认证）

**路径参数**:
- `postId`: 帖子ID (Guid)

**响应**: `200 OK`

### 9. 获取用户帖子列表

**接口**: `GET /api/posts/user/{userId}`

**描述**: 获取指定用户的帖子列表

**路径参数**:
- `userId`: 用户ID (Guid)

**请求参数**:
| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | int | 否 | 0 | 页码 |
| pageSize | int | 否 | 20 | 每页数量 |

**响应**: 返回分页的 `PostResponse[]`

## 评论管理 API

### 1. 获取帖子评论列表

**接口**: `GET /api/comments/post/{postId}`

**描述**: 获取指定帖子的评论列表

**路径参数**:
- `postId`: 帖子ID (Guid)

**请求参数**:
| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | int | 否 | 0 | 页码 |
| pageSize | int | 否 | 20 | 每页数量 |

**响应**: 返回 `CommentResponse[]` 数组

### 2. 获取评论详情

**接口**: `GET /api/comments/{commentId}`

**描述**: 获取指定评论的详细信息

**路径参数**:
- `commentId`: 评论ID (Guid)

**响应示例**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "评论内容",
  "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "parentCommentId": null,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": null,
  "likeCount": 5
}
```

### 3. 添加评论

**接口**: `POST /api/comments`

**描述**: 添加新评论（需要认证）

**请求体**:
```json
{
  "postId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "评论内容",
  "parentCommentId": null
}
```

**验证规则**:
- `postId`: 必填，帖子ID
- `content`: 必填，最大2000字符
- `parentCommentId`: 可选，父评论ID（用于回复）

**响应**: 返回创建的 `CommentResponse`

### 4. 编辑评论

**接口**: `PUT /api/comments/{commentId}`

**描述**: 编辑指定评论（需要认证，仅作者可编辑）

**路径参数**:
- `commentId`: 评论ID (Guid)

**请求体**:
```json
{
  "content": "新的评论内容"
}
```

**响应**: 返回更新后的 `CommentResponse`

### 5. 删除评论

**接口**: `DELETE /api/comments/{commentId}`

**描述**: 删除指定评论（需要认证，仅作者可删除）

**路径参数**:
- `commentId`: 评论ID (Guid)

**响应**: `204 No Content`

### 6. 点赞/取消点赞评论

**接口**: `POST /api/comments/{commentId}/like`

**描述**: 切换评论的点赞状态（需要认证）

**路径参数**:
- `commentId`: 评论ID (Guid)

**响应**: `200 OK`

## 数据模型

### PostResponse
```json
{
  "id": "Guid",
  "title": "string",
  "content": "string", 
  "titleImageBase64": "string?",
  "category": "PostCategory",
  "authorId": "Guid",
  "createdAt": "DateTime",
  "updatedAt": "DateTime?",
  "viewCount": "int",
  "likeCount": "int",
  "commentCount": "int",
  "favoriteCount": "int",
  "tags": "string[]"
}
```

### CommentResponse
```json
{
  "id": "Guid",
  "content": "string",
  "authorId": "Guid", 
  "parentCommentId": "Guid?",
  "createdAt": "DateTime",
  "updatedAt": "DateTime?",
  "likeCount": "int"
}
```

### PagedResponse<T>
```json
{
  "data": "T[]",
  "pageIndex": "int",
  "pageSize": "int",
  "totalCount": "int",
  "totalPages": "int",
  "hasPreviousPage": "bool",
  "hasNextPage": "bool"
}
```

## 错误响应

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "0HMV7M6QJN5QJ:00000001",
  "errors": {
    "Title": ["标题不能为空"]
  }
}
```

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "0HMV7M6QJN5QJ:00000001"
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "traceId": "0HMV7M6QJN5QJ:00000001"
}
```

## 状态码说明

- `200 OK` - 请求成功
- `201 Created` - 创建成功
- `204 No Content` - 删除成功
- `400 Bad Request` - 请求参数错误
- `401 Unauthorized` - 未认证或认证失败
- `403 Forbidden` - 无权限访问
- `404 Not Found` - 资源不存在
- `500 Internal Server Error` - 服务器内部错误

## 注意事项

1. **分页**: 所有分页接口都从0开始计数
2. **缓存**: 服务可选启用Redis缓存（如配置），用于热门帖子和帖子详情
3. **权限**: 只有帖子和评论的作者才能编辑或删除自己的内容
4. **字符限制**: 标题最大200字符，内容最大10000字符，评论最大2000字符
5. **软删除**: 删除操作采用软删除，数据不会真正从数据库移除
6. **Swagger 文档**: 开发环境访问 `http://localhost:9292/swagger` 查看并调试接口

