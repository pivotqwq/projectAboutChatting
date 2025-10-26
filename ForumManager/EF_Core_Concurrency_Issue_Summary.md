# EF Core 并发冲突异常（DbUpdateConcurrencyException）技术总结

## 📋 问题概述

### 错误现象

```
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException: 
The database operation was expected to affect 1 row(s), 
but actually affected 0 row(s); 
data may have been modified or deleted since entities were loaded.
```

### 触发场景

在 Forum 服务的点赞功能中，调用 `TogglePostLikeAsync` 时，系统报 500 错误。

---

## 🔍 问题根本原因

### 原因 1：Record 类型与 EF Core 的不兼容

#### ❌ 问题代码

```csharp
public record Post : IAggregateRoot
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    // ... 其他属性
}
```

#### 为什么会出问题？

| 特性 | Class | Record | EF Core 需求 |
|------|-------|--------|-------------|
| **相等性** | 引用相等（同一对象） | 值相等（内容相同即相等） | 引用相等 |
| **可变性** | 可变 | 不可变（设计理念） | 需要可变 |
| **变更跟踪** | 基于引用跟踪 | 难以跟踪 | 必须能跟踪 |
| **哈希码** | 基于引用 | 基于值 | 基于引用 |

#### ✅ 解决方案

```csharp
// 将所有 EF Core 实体从 record 改为 class
public class Post : IAggregateRoot
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    // ... 其他属性
}
```

**适用规则**：
- ✅ **EF Core 实体** → 使用 `class`
- ✅ **值对象/DTO** → 可以使用 `record`
- ✅ **不可变数据** → 可以使用 `record`

---

### 原因 2：缓存反序列化导致实体脱离跟踪

#### ❌ 问题代码

```csharp
public async Task<Post?> GetPostByIdAsync(Guid postId)
{
    // 从缓存获取
    var cachedPost = await _cache.GetStringAsync(cacheKey);
    if (!string.IsNullOrEmpty(cachedPost))
    {
        // ❌ 反序列化的对象不在 EF Core 跟踪中！
        return JsonSerializer.Deserialize<Post>(cachedPost);
    }
    
    // 从数据库获取
    var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
    return post;
}

public async Task<Post> UpdatePostAsync(Post post)
{
    _context.Posts.Update(post);  // ❌ 尝试更新未跟踪的实体
    await _context.SaveChangesAsync();  // 抛出并发异常
    return post;
}
```

#### 问题流程

```
1. 从缓存获取 Post（反序列化） → Post 对象（未被 EF Core 跟踪）
2. 修改 Post 的属性（如 LikeCount++）
3. 调用 _context.Posts.Update(post)
4. EF Core 发现这是一个未跟踪的对象
5. 尝试更新数据库，但找不到原始值
6. 抛出 DbUpdateConcurrencyException
```

#### ✅ 解决方案

```csharp
public async Task<Post?> GetPostByIdAsync(Guid postId)
{
    // ✅ 不使用缓存，直接从数据库获取，确保 EF Core 正确跟踪
    var post = await _context.Posts
        .Include(p => p.Comments)
        .Include(p => p.Likes)
        .Include(p => p.Favorites)
        .FirstOrDefaultAsync(p => p.Id == postId);
    
    return post;
}
```

---

### 原因 3：使用私有集合操作导航属性

#### ❌ 问题代码

```csharp
public class Post
{
    private readonly List<PostLike> _likes = new();
    public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();
    
    public void LikePost(Guid userId)
    {
        _likes.Add(new PostLike(Id, userId));  // ❌ 修改私有集合
        LikeCount++;
    }
}

// 使用时
var post = await GetPostByIdAsync(postId);
post.LikePost(userId);  // 修改了私有集合，但 EF Core 可能无法正确跟踪
await UpdatePostAsync(post);  // 更新失败
```

#### ✅ 解决方案

```csharp
// 直接在 Repository 层操作数据库表
public async Task TogglePostLikeAsync(Guid postId, Guid userId)
{
    var existingLike = await _context.PostLikes
        .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

    if (existingLike != null)
    {
        // 取消点赞
        _context.PostLikes.Remove(existingLike);
        
        // 更新帖子点赞数
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
        }
    }
    else
    {
        // 添加点赞
        var newLike = new PostLike(postId, userId);
        _context.PostLikes.Add(newLike);
        
        // 更新帖子点赞数
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            post.LikeCount++;
        }
    }

    await _context.SaveChangesAsync();
}
```

---

## 🎯 完整解决方案

### 1. 实体定义修改

```csharp
// ❌ 错误
public record Post : IAggregateRoot { }

// ✅ 正确
public class Post : IAggregateRoot { }
```

### 2. 移除缓存实体

```csharp
// ❌ 错误：缓存完整实体
public async Task<Post?> GetPostByIdAsync(Guid postId)
{
    var cached = await _cache.GetStringAsync(key);
    if (cached != null)
        return JsonSerializer.Deserialize<Post>(cached); // 脱离跟踪
    
    var post = await _context.Posts.FirstOrDefaultAsync(...);
    await _cache.SetStringAsync(key, JsonSerializer.Serialize(post)); // 缓存实体
    return post;
}

// ✅ 正确：不缓存实体，或只缓存 DTO
public async Task<Post?> GetPostByIdAsync(Guid postId)
{
    // 直接从数据库获取，确保被 EF Core 跟踪
    return await _context.Posts
        .Include(p => p.Comments)
        .Include(p => p.Likes)
        .FirstOrDefaultAsync(p => p.Id == postId);
}
```

### 3. 优化 Update 方法

```csharp
// ❌ 问题：直接调用 Update
public async Task<Post> UpdatePostAsync(Post post)
{
    _context.Posts.Update(post);  // 可能导致问题
    await _context.SaveChangesAsync();
    return post;
}

// ✅ 改进：检查实体状态
public async Task<Post> UpdatePostAsync(Post post)
{
    var entry = _context.Entry(post);
    if (entry.State == EntityState.Detached)
    {
        _context.Posts.Update(post);
    }
    // 如果已被跟踪，直接保存即可
    await _context.SaveChangesAsync();
    return post;
}
```

### 4. 重构点赞逻辑

```csharp
// ❌ 问题：通过领域实体的私有集合操作
public async Task TogglePostLikeAsync(Guid postId, Guid userId)
{
    var post = await GetPostByIdAsync(postId);
    post.LikePost(userId);  // 修改私有集合
    await UpdatePostAsync(post);  // 可能失败
}

// ✅ 正确：直接在 Repository 层操作关系表
public async Task TogglePostLikeAsync(Guid postId, Guid userId)
{
    var existingLike = await _context.PostLikes
        .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

    if (existingLike != null)
    {
        _context.PostLikes.Remove(existingLike);
        var post = await _context.Posts.FindAsync(postId);
        if (post != null) post.LikeCount--;
    }
    else
    {
        _context.PostLikes.Add(new PostLike(postId, userId));
        var post = await _context.Posts.FindAsync(postId);
        if (post != null) post.LikeCount++;
    }

    await _context.SaveChangesAsync();
}
```

---

## 💡 EF Core 最佳实践

### 1. 实体类型选择

| 场景 | 推荐类型 | 原因 |
|------|---------|------|
| 数据库实体（Entity） | `class` | 需要引用相等性和变更跟踪 |
| 值对象（Value Object） | `record` | 不可变，基于值相等 |
| DTO（Data Transfer Object） | `record` 或 `class` | 不需要跟踪，传输用 |
| 请求/响应模型 | `record` | 不可变，值传递 |

### 2. 缓存策略

#### ❌ 不应该缓存的

- ✗ 带导航属性的实体
- ✗ 需要被 Change Tracker 跟踪的对象
- ✗ 包含私有集合的聚合根
- ✗ 会被修改后保存的实体

#### ✅ 可以缓存的

- ✓ DTO（只读数据传输对象）
- ✓ 聚合后的统计数据
- ✓ 查询结果的 ID 列表
- ✓ 配置信息、元数据

#### 正确的缓存方式

```csharp
// 方式 1：缓存 DTO 而不是实体
public async Task<PostDTO?> GetPostDTOAsync(Guid postId)
{
    var cached = await _cache.GetStringAsync(key);
    if (cached != null)
        return JsonSerializer.Deserialize<PostDTO>(cached); // ✓ DTO 可以缓存
    
    var post = await _context.Posts.FirstOrDefaultAsync(...);
    var dto = MapToDTO(post);
    await _cache.SetStringAsync(key, JsonSerializer.Serialize(dto));
    return dto;
}

// 方式 2：缓存 ID 列表
public async Task<List<Post>> GetHotPostsAsync(int count)
{
    var cachedIds = await _cache.GetStringAsync("hot_post_ids");
    List<Guid> postIds;
    
    if (cachedIds != null)
    {
        postIds = JsonSerializer.Deserialize<List<Guid>>(cachedIds);
    }
    else
    {
        postIds = await _context.Posts
            .OrderByDescending(...)
            .Select(p => p.Id)
            .Take(count)
            .ToListAsync();
        await _cache.SetStringAsync("hot_post_ids", JsonSerializer.Serialize(postIds));
    }
    
    // 从数据库获取完整实体（被跟踪）
    return await _context.Posts.Where(p => postIds.Contains(p.Id)).ToListAsync();
}
```

### 3. 实体状态管理

#### EF Core 实体状态

```csharp
public enum EntityState
{
    Detached,      // 未被跟踪
    Unchanged,     // 已跟踪，未修改
    Deleted,       // 标记为删除
    Modified,      // 已跟踪，已修改
    Added          // 新增
}
```

#### 检查和处理实体状态

```csharp
// 检查实体是否被跟踪
var entry = _context.Entry(entity);
switch (entry.State)
{
    case EntityState.Detached:
        // 未跟踪，需要 Attach 或 Update
        _context.Entity.Update(entity);
        break;
    case EntityState.Modified:
        // 已修改，直接保存即可
        break;
    case EntityState.Unchanged:
        // 未修改，不需要保存
        break;
}

await _context.SaveChangesAsync();
```

### 4. 导航属性和关系管理

#### ❌ 问题模式：通过私有集合管理

```csharp
public class Post
{
    private readonly List<PostLike> _likes = new();
    public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();
    
    public void LikePost(Guid userId)
    {
        _likes.Add(new PostLike(Id, userId));  // ❌ EF Core 可能无法跟踪
        LikeCount++;
    }
}
```

**问题**：
1. 私有集合对 EF Core 不透明
2. 反序列化后私有集合为空
3. 变更跟踪不可靠

#### ✅ 推荐模式 1：直接操作 DbSet

```csharp
// 在 Repository 中直接操作关系表
public async Task TogglePostLikeAsync(Guid postId, Guid userId)
{
    var like = await _context.PostLikes
        .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

    if (like != null)
    {
        _context.PostLikes.Remove(like);
        await UpdateLikeCount(postId, -1);
    }
    else
    {
        _context.PostLikes.Add(new PostLike(postId, userId));
        await UpdateLikeCount(postId, +1);
    }

    await _context.SaveChangesAsync();
}

private async Task UpdateLikeCount(Guid postId, int delta)
{
    var post = await _context.Posts.FindAsync(postId);
    if (post != null)
    {
        post.LikeCount = Math.Max(0, post.LikeCount + delta);
    }
}
```

#### ✅ 推荐模式 2：使用公开的导航属性

```csharp
public class Post
{
    // 使用公开的导航属性，让 EF Core 管理
    public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    
    public void LikePost(Guid userId)
    {
        if (!Likes.Any(l => l.UserId == userId))
        {
            Likes.Add(new PostLike(Id, userId));
            LikeCount++;
        }
    }
}

// 查询时必须 Include
var post = await _context.Posts
    .Include(p => p.Likes)  // 必须 Include
    .FirstOrDefaultAsync(p => p.Id == postId);
```

---

## 🛠️ 调试技巧

### 1. 检查实体跟踪状态

```csharp
var entry = _context.Entry(entity);
Console.WriteLine($"实体状态: {entry.State}");
Console.WriteLine($"是否被跟踪: {entry.State != EntityState.Detached}");

// 查看所有被跟踪的实体
var trackedEntities = _context.ChangeTracker.Entries()
    .Where(e => e.State != EntityState.Detached)
    .ToList();

foreach (var e in trackedEntities)
{
    Console.WriteLine($"{e.Entity.GetType().Name}: {e.State}");
}
```

### 2. 启用详细日志

```csharp
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.ChangeTracking": "Debug"
    }
  }
}
```

### 3. 使用 AsNoTracking 查询只读数据

```csharp
// 只读查询，不需要跟踪
var posts = await _context.Posts
    .AsNoTracking()  // 提高性能，不跟踪
    .Where(p => p.Status == PostStatus.Published)
    .ToListAsync();

// 需要更新的查询，必须跟踪
var post = await _context.Posts
    .FirstOrDefaultAsync(p => p.Id == postId);  // 默认跟踪
```

---

## 📊 常见 EF Core 错误对比

| 错误类型 | 原因 | 解决方案 |
|---------|------|---------|
| **DbUpdateConcurrencyException** | 实体未被跟踪或并发冲突 | 确保实体被跟踪，使用并发令牌 |
| **InvalidOperationException (tracking)** | 同一实体被跟踪多次 | 使用 AsNoTracking 或检查状态 |
| **NullReferenceException (navigation)** | 未 Include 导航属性 | 使用 Include/ThenInclude |
| **SqlException (FK violation)** | 外键约束违反 | 检查关联数据存在性 |

---

## ✅ 修复清单

### Forum 服务修复项

- [x] **实体类型修改**
  - Post: `record` → `class`
  - Comment: `record` → `class`
  - PostLike: `record` → `class`
  - PostFavorite: `record` → `class`
  - CommentLike: `record` → `class`

- [x] **移除缓存**
  - GetPostByIdAsync：移除实体缓存
  - GetHotPostsAsync：移除实体缓存

- [x] **重构点赞逻辑**
  - 添加 `TogglePostLikeAsync` 到 Repository
  - 添加 `TogglePostFavoriteAsync` 到 Repository
  - 添加 `ToggleCommentLikeAsync` 到 Repository
  - DomainService 调用 Repository 的方法

- [x] **优化 Update 方法**
  - 检查实体状态后再 Update

- [x] **数据库配置修复**
  - 连接字符串：Username 改为 `postgres`
  - 密码统一为：`WYCHCnt4E2WAbALh`
  - JWT 配置统一

---

## 📚 相关知识点

### 1. EF Core Change Tracker

**Change Tracker** 是 EF Core 的核心组件，负责：
- 跟踪实体状态
- 检测属性变化
- 生成 SQL 更新语句
- 管理并发冲突

### 2. 实体生命周期

```
[New Entity]
    ↓ Add()
[Added State]
    ↓ SaveChanges()
[Unchanged State] ←─ 从数据库查询
    ↓ 修改属性
[Modified State]
    ↓ SaveChanges()
[Unchanged State]
    ↓ Remove()
[Deleted State]
    ↓ SaveChanges()
[Detached State]
```

### 3. Include 与延迟加载

```csharp
// 急切加载（Eager Loading）
var post = await _context.Posts
    .Include(p => p.Comments)          // 一次性加载
    .Include(p => p.Likes)
    .FirstOrDefaultAsync(p => p.Id == id);

// 延迟加载（Lazy Loading）- 需要配置
var post = await _context.Posts.FindAsync(id);
var comments = post.Comments;  // 此时才查询数据库（需要 virtual 关键字）

// 显式加载（Explicit Loading）
var post = await _context.Posts.FindAsync(id);
await _context.Entry(post).Collection(p => p.Comments).LoadAsync();
```

### 4. 并发控制

#### 乐观并发控制

```csharp
public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    
    [Timestamp]  // 或 [ConcurrencyCheck]
    public byte[] RowVersion { get; set; }
}

// EF Core 会自动检查 RowVersion
// 如果不匹配，抛出 DbUpdateConcurrencyException
```

#### 处理并发异常

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
    {
        if (entry.Entity is Post)
        {
            // 获取数据库中的当前值
            var databaseValues = await entry.GetDatabaseValuesAsync();
            
            if (databaseValues == null)
            {
                // 数据已被删除
            }
            else
            {
                // 数据已被其他用户修改
                // 可以选择：重新加载、合并更改、提示用户等
                entry.OriginalValues.SetValues(databaseValues);
            }
        }
    }
}
```

---

## 🎓 学到的教训

### 1. Record vs Class

- ✅ **在 DDD 中**：实体用 `class`，值对象用 `record`
- ✅ **在 EF Core 中**：实体必须用 `class`
- ✅ **在 API 层**：DTO 可以用 `record`

### 2. 缓存与 ORM

- ✅ **缓存规则**：只缓存不可变数据或 DTO
- ✅ **实体规则**：永远不要缓存被 ORM 跟踪的实体
- ✅ **序列化规则**：反序列化的对象与原对象不是同一引用

### 3. 领域模型设计

- ✅ **聚合根**：控制边界内的一致性
- ✅ **仓储职责**：数据访问和持久化
- ✅ **私有集合**：封装性 vs ORM 透明性的权衡

### 4. 性能优化

```csharp
// ❌ 差：每次都查询导航属性
var post = await _context.Posts.Include(p => p.Likes).FirstOrDefaultAsync();

// ✅ 好：只在需要时查询
var post = await _context.Posts.FindAsync(id);
var likeCount = await _context.PostLikes.CountAsync(pl => pl.PostId == id);

// ✅ 更好：使用冗余字段（LikeCount）
var post = await _context.Posts.FindAsync(id);
var likes = post.LikeCount;  // 不需要查询关联表
```

---

## 🚀 部署检查清单

### 修复后的部署步骤

#### 1. 数据库准备
```bash
# 创建数据库
PGPASSWORD=WYCHCnt4E2WAbALh psql -h localhost -U postgres -c 'CREATE DATABASE "ForumDB";'

# 执行建表脚本
PGPASSWORD=WYCHCnt4E2WAbALh psql -h localhost -U postgres -d ForumDB -f create_forum_database.sql

# 验证表已创建
PGPASSWORD=WYCHCnt4E2WAbALh psql -h localhost -U postgres -d ForumDB -c '\dt'
```

#### 2. 代码编译
```bash
cd /path/to/ForumManager/ForumManager.WebAPI
dotnet clean
dotnet build -c Release
```

#### 3. 运行服务
```bash
dotnet run --urls "http://0.0.0.0:9292"
```

#### 4. 测试接口
```bash
# 获取帖子列表
curl http://localhost:9292/api/Posts

# 点赞功能（需要 token）
curl -X POST http://localhost:9292/api/Posts/{postId}/like \
  -H "Authorization: Bearer {token}"
```

---

## 🔗 参考资源

### 官方文档

- [EF Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [DbUpdateConcurrencyException](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception)
- [Entity States](https://docs.microsoft.com/en-us/ef/core/change-tracking/entity-entries)

### 相关问题

- Stack Overflow: "EF Core DbUpdateConcurrencyException"
- GitHub Issues: [Entity Framework Core #issues](https://github.com/dotnet/efcore/issues)

---

## 📝 总结

### 核心要点

1. **EF Core 实体必须使用 `class`**，不能用 `record`
2. **不要缓存 ORM 跟踪的实体**，应该缓存 DTO 或 ID
3. **反序列化的对象脱离 EF Core 跟踪**，会导致更新失败
4. **关系操作应在 Repository 层直接处理**，而不是通过私有集合
5. **使用 `FindAsync` 或查询可以确保实体被跟踪**

### 最佳实践

```csharp
// ✅ 推荐的架构
[Controller] 
    → [DomainService]  // 业务逻辑
        → [Repository]  // 数据访问
            → [DbContext]  // EF Core 上下文

// ✅ 推荐的数据流
1. Repository 从数据库查询实体（被跟踪）
2. DomainService 执行业务逻辑
3. Repository 保存更改（自动检测）
4. 返回 DTO 给 Controller（不返回实体）
```

---

**文档版本**: v1.0  
**最后更新**: 2025-10-18  
**适用项目**: ForumAndChatRoomProject  
**技术栈**: ASP.NET Core 8.0, EF Core 8.0, PostgreSQL  

