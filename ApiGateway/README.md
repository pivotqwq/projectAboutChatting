# API Gateway

基于 .NET 8 和 YARP 的微服务 API 网关，提供统一入口、认证、限流、熔断和数据聚合功能。

## 功能特性

### 🔐 认证与授权
- JWT Token 验证
- 用户身份识别
- 请求头传递用户信息到下游服务
- 支持匿名访问的公开端点

### 🚦 限流与熔断
- 基于用户ID和IP的请求限流
- 突发请求限制
- Polly 重试和熔断策略
- 可配置的限流参数

### 📊 数据聚合
- 用户资料与帖子聚合
- 帖子与评论聚合
- 用户仪表板数据聚合
- 论坛统计信息聚合
- 智能缓存机制

### 🔄 反向代理
- YARP 高性能反向代理
- 动态路由配置
- 负载均衡支持
- 健康检查

### 📝 日志与监控
- 结构化日志（Serilog）
- 请求/响应日志
- 性能监控
- 错误追踪

## 项目结构

```
ApiGateway/
├── Controllers/          # API 控制器
│   └── AggregationController.cs
├── Middleware/           # 自定义中间件
│   ├── AuthenticationMiddleware.cs
│   ├── RateLimitMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Services/             # 服务层
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IRateLimitService.cs
│   ├── RateLimitService.cs
│   ├── IAggregationService.cs
│   └── AggregationService.cs
├── Program.cs            # 应用程序入口
├── appsettings.json      # 配置文件
├── Dockerfile           # Docker 配置
└── docker-compose.yml   # 容器编排
```

## 快速开始

### 本地开发

1. **克隆项目**
```bash
git clone <repository-url>
cd ApiGateway
```

2. **配置服务地址**
编辑 `appsettings.Development.json`：
```json
{
  "Services": {
    "UserManager": "http://localhost:5001",
    "ForumManager": "http://localhost:5002",
    "ChatService": "http://localhost:5003",
    "MatchingService": "http://localhost:5004"
  }
}
```

3. **运行应用**
```bash
dotnet run
```

### Docker 部署

1. **构建镜像**
```bash
docker build -t api-gateway:latest .
```

2. **运行容器**
```bash
docker run -d \
  --name api-gateway \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  api-gateway:latest
```

3. **使用 Docker Compose**
```bash
# 生产环境
docker-compose up -d

# 开发环境
docker-compose -f docker-compose.dev.yml up -d
```

## API 端点

### 聚合端点

#### 获取用户资料及帖子
```http
GET /api/aggregated/user/{userId}/profile-with-posts?page=1&pageSize=10
Authorization: Bearer <token>
```

#### 获取帖子及评论
```http
GET /api/aggregated/post/{postId}/with-comments?page=1&pageSize=10
Authorization: Bearer <token>
```

#### 获取用户仪表板
```http
GET /api/aggregated/user/dashboard
Authorization: Bearer <token>
```

#### 获取论坛统计
```http
GET /api/aggregated/forum/stats
Authorization: Bearer <token>
```

### 代理端点

- `/api/users/*` → UserManager 服务
- `/api/forum/*` → ForumManager 服务
- `/api/chat/*` → ChatService 服务
- `/api/matching/*` → MatchingService 服务

### 健康检查
```http
GET /api/aggregated/health
```

## 配置说明

### JWT 配置
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "ApiGateway",
    "Audience": "Microservices"
  }
}
```

### 限流配置
```json
{
  "RateLimit": {
    "RequestsPerMinute": 100,
    "BurstLimit": 10
  }
}
```

### 服务地址配置
```json
{
  "Services": {
    "UserManager": "http://localhost:5001",
    "ForumManager": "http://localhost:5002",
    "ChatService": "http://localhost:5003",
    "MatchingService": "http://localhost:5004"
  }
}
```

## 中间件说明

### 认证中间件
- 验证 JWT Token
- 提取用户信息
- 传递用户信息到下游服务
- 支持公开端点跳过认证

### 限流中间件
- 基于用户ID和IP的限流
- 每分钟请求数限制
- 突发请求限制
- 返回限流信息头

### 日志中间件
- 请求/响应日志
- 性能监控
- 慢请求检测
- 错误追踪

## 缓存策略

- **用户资料+帖子**: 5分钟
- **帖子+评论**: 3分钟
- **用户仪表板**: 2分钟
- **论坛统计**: 10分钟

## 监控与日志

### 日志级别
- **开发环境**: Debug
- **生产环境**: Information

### 日志输出
- 控制台输出
- 文件输出 (`logs/api-gateway-*.txt`)
- 结构化日志格式

### 监控指标
- 请求响应时间
- 错误率
- 限流触发次数
- 缓存命中率

## 安全考虑

1. **JWT 密钥管理**: 生产环境使用强密钥
2. **HTTPS 部署**: 生产环境启用 HTTPS
3. **CORS 配置**: 根据需要配置跨域策略
4. **限流保护**: 防止 API 滥用
5. **输入验证**: 验证所有输入参数

## 扩展功能

### 添加新的聚合端点
1. 在 `IAggregationService` 中添加方法
2. 在 `AggregationService` 中实现逻辑
3. 在 `AggregationController` 中添加端点

### 添加新的中间件
1. 创建中间件类
2. 在 `Program.cs` 中注册
3. 配置管道顺序

### 自定义限流策略
1. 修改 `RateLimitService`
2. 添加新的限流规则
3. 配置不同的限流参数

## 故障排除

### 常见问题

1. **服务连接失败**
   - 检查服务地址配置
   - 确认下游服务正常运行
   - 检查网络连接

2. **认证失败**
   - 验证 JWT 配置
   - 检查 Token 格式
   - 确认 Token 未过期

3. **限流触发**
   - 检查请求频率
   - 调整限流参数
   - 查看日志详情

### 调试模式
```bash
# 启用详细日志
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

## 性能优化

1. **缓存策略**: 合理设置缓存时间
2. **并发处理**: 使用异步编程
3. **连接池**: 配置 HTTP 客户端连接池
4. **压缩**: 启用响应压缩
5. **监控**: 持续监控性能指标

## 许可证

本项目采用 MIT 许可证。
