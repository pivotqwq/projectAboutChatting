# MatchingService API 接口文档

## 基础信息

- **Base URL**: `/api`
- **数据格式**: JSON
- **编码**: UTF-8
- **认证方式**: JWT Bearer Token

---

## 目录

1. [标签管理接口](#标签管理接口)
2. [用户标签接口](#用户标签接口)
3. [推荐服务接口](#推荐服务接口)
4. [用户交互接口](#用户交互接口)

---

## 标签管理接口

### 1. 获取所有标签

**接口地址**: `GET /api/Tags`

**接口描述**: 获取系统中的所有标签列表

**认证要求**: 需要JWT Token

**请求参数**:
- `category` (可选): 标签分类筛选 (1=运动, 2=游戏, 3=学习, 4=美食, 5=旅游, 6=音乐, 7=科技, 8=时尚, 9=读书, 99=其他)

**请求示例**:
```
GET /api/Tags?category=1
```

**成功响应** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "篮球",
    "description": "篮球运动相关",
    "category": 1,
    "usageCount": 156,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastUsedAt": "2024-01-15T10:30:00Z",
    "isActive": true
  }
]
```

---

### 2. 获取热门标签

**接口地址**: `GET /api/Tags/popular`

**接口描述**: 获取使用次数最多的热门标签

**认证要求**: 需要JWT Token

**请求参数**:
- `count` (可选): 返回数量，默认20

**请求示例**:
```
GET /api/Tags/popular?count=10
```

**成功响应** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "篮球",
    "description": "篮球运动相关",
    "category": 1,
    "usageCount": 256,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastUsedAt": "2024-01-15T10:30:00Z",
    "isActive": true
  }
]
```

---

### 3. 搜索标签

**接口地址**: `GET /api/Tags/search`

**接口描述**: 根据关键词搜索标签

**认证要求**: 需要JWT Token

**请求参数**:
- `keyword` (必填): 搜索关键词
- `count` (可选): 返回数量，默认10

**请求示例**:
```
GET /api/Tags/search?keyword=篮球&count=5
```

**成功响应** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "篮球",
    "description": "篮球运动相关",
    "category": 1,
    "usageCount": 156,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastUsedAt": "2024-01-15T10:30:00Z",
    "isActive": true
  }
]
```

---

### 4. 创建标签

**接口地址**: `POST /api/Tags`

**接口描述**: 创建新的标签

**认证要求**: 需要JWT Token

**请求参数**:

```json
{
  "name": "新标签名称",
  "description": "标签描述",
  "category": 1
}
```

**请求示例**:

```json
{
  "name": "网球",
  "description": "网球运动相关",
  "category": 1
}
```

**成功响应** (201 Created):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "网球",
  "description": "网球运动相关",
  "category": 1,
  "usageCount": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUsedAt": null,
  "isActive": true
}
```

**错误响应**:
- `400 Bad Request`: "标签名称不能为空"
- `400 Bad Request`: "标签名已存在"

---

## 用户标签接口

### 1. 获取用户标签

**接口地址**: `GET /api/users/{userId}/UserTags`

**接口描述**: 获取指定用户的标签列表

**认证要求**: 需要JWT Token

**请求示例**:
```
GET /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/UserTags
```

**成功响应** (200 OK):
```json
[
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tagId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    "weight": 0.8,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastUpdatedAt": "2024-01-10T00:00:00Z",
    "isActive": true,
    "tag": {
      "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
      "name": "篮球",
      "description": "篮球运动相关",
      "category": 1
    }
  }
]
```

---

### 2. 添加用户标签

**接口地址**: `POST /api/users/{userId}/UserTags`

**接口描述**: 为用户添加标签

**认证要求**: 需要JWT Token

**请求参数**:
```json
{
  "tagId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "weight": 0.8
}
```

**请求示例**:
```json
{
  "tagId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "weight": 0.8
}
```

**成功响应** (201 Created):
```json
{
  "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tagId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "weight": 0.8,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUpdatedAt": "2024-01-15T10:30:00Z",
  "isActive": true
}
```

**错误响应**:
- `400 Bad Request`: "标签权重必须在0-1之间"
- `400 Bad Request`: "标签不存在"

---

### 3. 移除用户标签

**接口地址**: `DELETE /api/users/{userId}/UserTags/{tagId}`

**接口描述**: 移除用户的标签

**认证要求**: 需要JWT Token

**请求示例**:
```
DELETE /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/UserTags/5fa85f64-5717-4562-b3fc-2c963f66afa8
```

**成功响应** (204 No Content):

**错误响应**:
- `403 Forbidden`: "只能移除自己的标签"

---

## 推荐服务接口

### 1. 获取用户推荐

**接口地址**: `GET /api/Recommendations/{userId}`

**接口描述**: 获取指定用户的推荐列表（混合推荐算法）

**认证要求**: 需要JWT Token

**请求参数**:
- `count` (可选): 返回数量，默认20
- `latitude` (可选): 用户纬度，用于地理位置推荐
- `longitude` (可选): 用户经度，用于地理位置推荐

**请求示例**:
```
GET /api/Recommendations/3fa85f64-5717-4562-b3fc-2c963f66afa6?count=10&latitude=39.9042&longitude=116.4074
```

**成功响应** (200 OK):
```json
[
  {
    "userId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "score": 0.85,
    "matchType": 5,
    "reason": "混合推荐算法",
    "metadata": {
      "tagSimilarity": 0.8,
      "locationDistance": 2.5,
      "collaborativeScore": 0.9
    }
  }
]
```

---

### 2. 获取基于标签的推荐

**接口地址**: `GET /api/Recommendations/{userId}/tag-based`

**接口描述**: 获取基于标签相似度的推荐

**认证要求**: 需要JWT Token

**请求参数**:
- `count` (可选): 返回数量，默认20

**请求示例**:
```
GET /api/Recommendations/3fa85f64-5717-4562-b3fc-2c963f66afa6/tag-based?count=10
```

**成功响应** (200 OK):
```json
[
  {
    "userId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "score": 0.8,
    "matchType": 1,
    "reason": "基于标签相似度",
    "metadata": {
      "commonTags": 5,
      "similarityScore": 0.8
    }
  }
]
```

---

### 3. 获取协同过滤推荐

**接口地址**: `GET /api/Recommendations/{userId}/collaborative`

**接口描述**: 获取基于协同过滤的推荐

**认证要求**: 需要JWT Token

**请求参数**:
- `count` (可选): 返回数量，默认20

**请求示例**:
```
GET /api/Recommendations/3fa85f64-5717-4562-b3fc-2c963f66afa6/collaborative?count=10
```

**成功响应** (200 OK):
```json
[
  {
    "userId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "score": 0.9,
    "matchType": 2,
    "reason": "协同过滤推荐",
    "metadata": {
      "similarUsers": 15,
      "predictedRating": 4.5
    }
  }
]
```

---

### 4. 获取地理位置推荐

**接口地址**: `GET /api/Recommendations/{userId}/location`

**接口描述**: 获取基于地理位置的推荐

**认证要求**: 需要JWT Token

**请求参数**:
- `latitude` (必填): 用户纬度
- `longitude` (必填): 用户经度
- `maxDistanceKm` (可选): 最大距离（公里），默认50
- `count` (可选): 返回数量，默认20

**请求示例**:
```
GET /api/Recommendations/3fa85f64-5717-4562-b3fc-2c963f66afa6/location?latitude=39.9042&longitude=116.4074&maxDistanceKm=10&count=5
```

**成功响应** (200 OK):
```json
[
  {
    "userId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "score": 0.7,
    "matchType": 3,
    "reason": "基于地理位置",
    "metadata": {
      "distance": 2.5,
      "city": "北京市",
      "province": "北京市"
    }
  }
]
```

---

## 用户交互接口

### 1. 记录用户交互

**接口地址**: `POST /api/Interactions`

**接口描述**: 记录用户间的交互行为

**认证要求**: 需要JWT Token

**请求参数**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetUserId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "type": 1,
  "rating": 4.5,
  "context": "查看了用户资料"
}
```

**交互类型说明**:
- `1`: 查看资料
- `2`: 发送消息
- `3`: 点赞
- `4`: 关注
- `5`: 评论
- `6`: 分享
- `7`: 举报
- `8`: 加入群组
- `9`: 参与活动

**请求示例**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetUserId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "type": 3,
  "rating": 5.0,
  "context": "点赞了用户"
}
```

**成功响应** (201 Created):
```json
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afaa",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetUserId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "type": 3,
  "rating": 5.0,
  "createdAt": "2024-01-15T10:30:00Z",
  "context": "点赞了用户",
  "metadata": null
}
```

**错误响应**:
- `400 Bad Request`: "评分必须在0-5之间"
- `400 Bad Request`: "交互类型无效"

---

### 2. 获取用户交互历史

**接口地址**: `GET /api/users/{userId}/interactions`

**接口描述**: 获取指定用户的交互历史记录

**认证要求**: 需要JWT Token

**请求参数**:
- `type` (可选): 交互类型筛选
- `targetUserId` (可选): 目标用户ID筛选
- `page` (可选): 页码，默认1
- `pageSize` (可选): 每页数量，默认20

**请求示例**:
```
GET /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/interactions?type=3&page=1&pageSize=10
```

**成功响应** (200 OK):
```json
{
  "items": [
    {
      "id": "7fa85f64-5717-4562-b3fc-2c963f66afaa",
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "targetUserId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
      "type": 3,
      "rating": 5.0,
      "createdAt": "2024-01-15T10:30:00Z",
      "context": "点赞了用户",
      "metadata": null
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

---

## 用户匹配接口

### 1. 获取用户匹配列表

**接口地址**: `GET /api/users/{userId}/matches`

**接口描述**: 获取指定用户的匹配记录列表

**认证要求**: 需要JWT Token

**请求参数**:
- `status` (可选): 匹配状态筛选 (1=待处理, 2=已接受, 3=已拒绝, 4=已过期, 5=已取消)
- `matchType` (可选): 匹配类型筛选 (1=基于标签, 2=协同过滤, 3=地理位置, 4=基于活跃度, 5=混合推荐, 6=随机推荐)
- `page` (可选): 页码，默认1
- `pageSize` (可选): 每页数量，默认20

**请求示例**:
```
GET /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/matches?status=2&page=1&pageSize=10
```

**成功响应** (200 OK):
```json
{
  "items": [
    {
      "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "matchedUserId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
      "matchScore": 0.85,
      "matchType": 5,
      "status": 2,
      "createdAt": "2024-01-15T10:30:00Z",
      "lastInteractionAt": "2024-01-15T11:00:00Z",
      "notes": "共同兴趣：篮球、编程"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 10,
  "totalPages": 2
}
```

---

### 2. 更新匹配状态

**接口地址**: `PUT /api/matches/{matchId}/status`

**接口描述**: 更新匹配记录的状态

**认证要求**: 需要 JWT Token

**请求参数**:
```json
{
  "status": 2,
  "notes": "接受匹配，开始聊天"
}
```

**状态说明**:
- `1`: 待处理
- `2`: 已接受
- `3`: 已拒绝
- `4`: 已过期
- `5`: 已取消

**请求示例**:
```json
{
  "status": 2,
  "notes": "接受匹配，开始聊天"
}
```

**成功响应** (200 OK):
```json
{
  "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "matchedUserId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
  "matchScore": 0.85,
  "matchType": 5,
  "status": 2,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastInteractionAt": "2024-01-15T11:00:00Z",
  "notes": "接受匹配，开始聊天"
}
```

**错误响应**:
- `400 Bad Request`: "无效的匹配状态"
- `403 Forbidden`: "只能更新自己的匹配记录"
- `404 Not Found`: "匹配记录不存在"

---

## 通用说明

### 认证方式

所有接口都需要JWT认证，需要在请求头中携带Token：

```
Authorization: Bearer {access_token}
```

### 响应状态码

- `200 OK`: 请求成功
- `201 Created`: 创建成功
- `204 No Content`: 删除成功
- `400 Bad Request`: 请求参数错误或业务逻辑错误
- `401 Unauthorized`: 未授权或Token无效
- `403 Forbidden`: 权限不足
- `404 Not Found`: 资源不存在
- `500 Internal Server Error`: 服务器内部错误

### 推荐算法说明

1. **基于标签的推荐**: 计算用户间的标签相似度，推荐标签重叠度高的用户
2. **协同过滤推荐**: 基于用户交互历史，找到相似用户，推荐他们喜欢的用户
3. **地理位置推荐**: 基于用户地理位置，推荐附近的用户
4. **混合推荐**: 综合以上三种算法的推荐结果

### 缓存机制

- 用户推荐结果缓存5分钟
- 热门标签缓存10分钟
- 用户标签关系缓存15分钟

### 性能优化

- 使用Redis缓存热门数据
- 推荐算法异步计算
- 数据库查询优化和索引

  
