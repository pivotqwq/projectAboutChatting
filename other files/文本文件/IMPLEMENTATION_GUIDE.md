# MatchingService 实现指南

## 概述

本文档详细说明了MatchingService项目中需要实现的功能和代码。项目采用Clean Architecture架构，分为Domain、Infrastructure和WebAPI三个层次。

## 项目结构分析

```
MatchingService/
├── MatchingService.Domain/           # 领域层
│   ├── Entities/                     # 实体
│   ├── IRepositories/               # 仓储接口
│   ├── Services/                    # 领域服务接口
│   └── ValueObjects/                # 值对象
├── MatchingService.Infrastructure/  # 基础设施层
│   ├── Configurations/              # EF配置
│   ├── Repositories/                # 仓储实现
│   └── Services/                    # 服务实现
└── MatchingService.WebAPI/          # Web API层
    ├── Controllers/                 # 控制器
    └── Program.cs                   # 启动配置
```

## 待实现功能清单

### 1. 仓储层实现 (Infrastructure/Repositories/)

#### 1.1 UserTagRepository.cs
```csharp
using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;

namespace MatchingService.Infrastructure.Repositories
{
    public class UserTagRepository : IUserTagRepository
    {
        private readonly MatchingDbContext _context;

        public UserTagRepository(MatchingDbContext context)
        {
            _context = context;
        }

        // 需要实现的方法：
        // - GetByUserAndTagAsync(Guid userId, Guid tagId)
        // - GetActiveUserTagsAsync(Guid userId)
        // - GetByTagIdAsync(Guid tagId)
        // - AddAsync(UserTag userTag)
        // - UpdateAsync(UserTag userTag)
        // - DeleteByUserAndTagAsync(Guid userId, Guid tagId)
        // - GetUserTagsByCategoryAsync(Guid userId, TagCategory category)
    }
}
```

#### 1.2 UserMatchRepository.cs
```csharp
using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;

namespace MatchingService.Infrastructure.Repositories
{
    public class UserMatchRepository : IUserMatchRepository
    {
        private readonly MatchingDbContext _context;

        public UserMatchRepository(MatchingDbContext context)
        {
            _context = context;
        }

        // 需要实现的方法：
        // - GetByUserPairAsync(Guid userId, Guid matchedUserId)
        // - GetUserMatchesAsync(Guid userId, MatchStatus? status = null)
        // - GetMatchesByTypeAsync(MatchType matchType)
        // - AddAsync(UserMatch userMatch)
        // - UpdateAsync(UserMatch userMatch)
        // - DeleteAsync(Guid id)
        // - GetMatchesWithPaginationAsync(Guid userId, int page, int pageSize)
    }
}
```

#### 1.3 UserInteractionRepository.cs
```csharp
using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;

namespace MatchingService.Infrastructure.Repositories
{
    public class UserInteractionRepository : IUserInteractionRepository
    {
        private readonly MatchingDbContext _context;

        public UserInteractionRepository(MatchingDbContext context)
        {
            _context = context;
        }

        // 需要实现的方法：
        // - GetUserInteractionsAsync(Guid userId, InteractionType? type = null)
        // - GetUserRatingsAsync(Guid userId)
        // - GetAllUsersAsync()
        // - AddAsync(UserInteraction interaction)
        // - GetInteractionsWithPaginationAsync(Guid userId, int page, int pageSize)
        // - GetInteractionStatsAsync(Guid userId)
    }
}
```

### 2. 服务层实现 (Infrastructure/Services/)

#### 2.1 RedisCacheService.cs
```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MatchingService.Domain.Entities;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        // 需要实现的方法：
        // - CachePopularTagsAsync(IEnumerable<Tag> tags, TimeSpan expiration)
        // - GetCachedPopularTagsAsync()
        // - CacheUserRecommendationsAsync(Guid userId, IEnumerable<RecommendationResult> recommendations, TimeSpan expiration)
        // - GetCachedUserRecommendationsAsync(Guid userId)
        // - ClearUserRecommendationsCacheAsync(Guid userId)
        // - CacheUserTagsAsync(Guid userId, IEnumerable<UserTag> userTags, TimeSpan expiration)
        // - GetCachedUserTagsAsync(Guid userId)
        // - ClearUserTagsCacheAsync(Guid userId)
    }
}
```

#### 2.2 RecommendationService.cs
```csharp
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IUserTagRepository _userTagRepository;
        private readonly IUserInteractionRepository _userInteractionRepository;
        private readonly ITagRepository _tagRepository;

        public RecommendationService(
            IUserTagRepository userTagRepository,
            IUserInteractionRepository userInteractionRepository,
            ITagRepository tagRepository)
        {
            _userTagRepository = userTagRepository;
            _userInteractionRepository = userInteractionRepository;
            _tagRepository = tagRepository;
        }

        // 需要实现的方法：
        // - GetTagBasedRecommendationsAsync(Guid userId, int count = 20)
        // - GetCollaborativeFilteringRecommendationsAsync(Guid userId, int count = 20)
        // - GetLocationBasedRecommendationsAsync(Guid userId, Location userLocation, double maxDistanceKm = 50, int count = 20)
        // - GetHybridRecommendationsAsync(Guid userId, Location? userLocation = null, int count = 20)
        // - CalculateUserSimilarityAsync(Guid userId1, Guid userId2)
        // - UpdateUserPreferenceModelAsync(Guid userId)
    }
}
```

### 3. 控制器实现 (WebAPI/Controllers/)

#### 3.1 InteractionsController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;
using System.Security.Claims;

namespace MatchingService.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InteractionsController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public InteractionsController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        // 需要实现的接口：
        // - POST /api/Interactions (记录用户交互)
        // - GET /api/users/{userId}/interactions (获取用户交互历史)
    }
}
```

#### 3.2 MatchesController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;
using System.Security.Claims;

namespace MatchingService.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public MatchesController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        // 需要实现的接口：
        // - GET /api/users/{userId}/matches (获取用户匹配列表)
        // - PUT /api/matches/{matchId}/status (更新匹配状态)
    }
}
```

### 4. 完善现有控制器

#### 4.1 TagsController 完善
需要完善的方法：
- `GetTag(Guid id)` - 获取单个标签
- `UpdateTag(Guid id, UpdateTagRequest request)` - 更新标签
- `DeleteTag(Guid id)` - 删除标签
- `GetTags` 方法中的分类筛选功能

#### 4.2 UserTagsController 完善
需要完善的方法：
- `GetUserTags(Guid userId)` - 获取用户标签列表
- `GetUserTag(Guid userId, Guid tagId)` - 获取单个用户标签
- `UpdateUserTag(Guid userId, Guid tagId, UpdateUserTagRequest request)` - 更新用户标签权重

#### 4.3 RecommendationsController 完善
需要完善的方法：
- `GetTagBasedRecommendations` - 基于标签的推荐
- `GetCollaborativeFilteringRecommendations` - 协同过滤推荐
- `GetLocationBasedRecommendations` - 地理位置推荐

## 实现步骤建议

### 第一阶段：基础仓储实现
1. 实现 UserTagRepository
2. 实现 UserMatchRepository
3. 实现 UserInteractionRepository
4. 更新 Program.cs 中的依赖注入

### 第二阶段：服务层实现
1. 实现 RedisCacheService
2. 实现 RecommendationService
3. 测试服务层功能

### 第三阶段：控制器完善
1. 完善现有控制器方法
2. 实现 InteractionsController
3. 实现 MatchesController
4. 添加错误处理和验证

### 第四阶段：功能增强
1. 添加分页查询支持
2. 完善错误处理
3. 添加日志记录
4. 性能优化

## 测试建议

### 单元测试
- 为每个仓储方法编写单元测试
- 为服务层方法编写单元测试
- 为控制器方法编写单元测试

### 集成测试
- 测试完整的API流程
- 测试数据库操作
- 测试缓存功能

## 注意事项

1. **错误处理**: 所有方法都应该有适当的错误处理
2. **数据验证**: 输入参数需要验证
3. **性能优化**: 数据库查询需要优化，避免N+1问题
4. **缓存策略**: 合理使用缓存提高性能
5. **安全性**: 确保用户只能访问自己的数据
6. **日志记录**: 重要操作需要记录日志

## 代码规范

1. 遵循C#编码规范
2. 使用async/await模式
3. 适当的异常处理
4. 清晰的注释和文档
5. 统一的命名规范

## 完成标准

- 所有接口都能正常响应
- 单元测试覆盖率 > 80%
- 集成测试通过
- 性能测试通过
- 代码审查通过
