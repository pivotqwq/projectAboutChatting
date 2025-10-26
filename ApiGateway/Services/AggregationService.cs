using System.Text.Json;

namespace ApiGateway.Services;

public class AggregationService : IAggregationService
{
    private readonly ILogger<AggregationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public AggregationService(
        ILogger<AggregationService> logger, 
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<JsonDocument> GetUserProfileWithPostsAsync(string userId, int page = 1, int pageSize = 10)
    {
        var cacheKey = $"user_profile_posts:{userId}:{page}:{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out JsonDocument? cachedResult))
        {
            return cachedResult!;
        }

        try
        {
            var userClient = _httpClientFactory.CreateClient("UserManager");
            var forumClient = _httpClientFactory.CreateClient("ForumManager");

            // 并行获取用户信息和帖子列表
            var userTask = userClient.GetAsync($"/api/users/{userId}");
            var postsTask = forumClient.GetAsync($"/api/forum/posts/user/{userId}?page={page}&pageSize={pageSize}");

            await Task.WhenAll(userTask, postsTask);

            var userResponse = await userTask;
            var postsResponse = await postsTask;

            if (!userResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get user info: {userResponse.StatusCode}");
            }

            var userJson = await userResponse.Content.ReadAsStringAsync();
            var postsJson = postsResponse.IsSuccessStatusCode 
                ? await postsResponse.Content.ReadAsStringAsync() 
                : "{\"posts\":[],\"totalCount\":0,\"page\":1,\"pageSize\":10}";

            // 合并数据
            var result = new
            {
                user = JsonSerializer.Deserialize<JsonElement>(userJson),
                posts = JsonSerializer.Deserialize<JsonElement>(postsJson),
                timestamp = DateTime.UtcNow
            };

            var resultJson = JsonSerializer.SerializeToDocument(result);
            
            // 缓存5分钟
            _cache.Set(cacheKey, resultJson, TimeSpan.FromMinutes(5));
            
            return resultJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate user profile with posts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<JsonDocument> GetPostWithCommentsAsync(string postId, int page = 1, int pageSize = 10)
    {
        var cacheKey = $"post_comments:{postId}:{page}:{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out JsonDocument? cachedResult))
        {
            return cachedResult!;
        }

        try
        {
            var forumClient = _httpClientFactory.CreateClient("ForumManager");

            // 并行获取帖子信息和评论
            var postTask = forumClient.GetAsync($"/api/forum/posts/{postId}");
            var commentsTask = forumClient.GetAsync($"/api/forum/posts/{postId}/comments?page={page}&pageSize={pageSize}");

            await Task.WhenAll(postTask, commentsTask);

            var postResponse = await postTask;
            var commentsResponse = await commentsTask;

            if (!postResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get post info: {postResponse.StatusCode}");
            }

            var postJson = await postResponse.Content.ReadAsStringAsync();
            var commentsJson = commentsResponse.IsSuccessStatusCode 
                ? await commentsResponse.Content.ReadAsStringAsync() 
                : "{\"comments\":[],\"totalCount\":0,\"page\":1,\"pageSize\":10}";

            // 合并数据
            var result = new
            {
                post = JsonSerializer.Deserialize<JsonElement>(postJson),
                comments = JsonSerializer.Deserialize<JsonElement>(commentsJson),
                timestamp = DateTime.UtcNow
            };

            var resultJson = JsonSerializer.SerializeToDocument(result);
            
            // 缓存3分钟
            _cache.Set(cacheKey, resultJson, TimeSpan.FromMinutes(3));
            
            return resultJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate post with comments for post {PostId}", postId);
            throw;
        }
    }

    public async Task<JsonDocument> GetUserDashboardAsync(string userId)
    {
        var cacheKey = $"user_dashboard:{userId}";
        
        if (_cache.TryGetValue(cacheKey, out JsonDocument? cachedResult))
        {
            return cachedResult!;
        }

        try
        {
            var userClient = _httpClientFactory.CreateClient("UserManager");
            var forumClient = _httpClientFactory.CreateClient("ForumManager");
            var matchingClient = _httpClientFactory.CreateClient("MatchingService");

            // 并行获取用户数据
            var userTask = userClient.GetAsync($"/api/users/{userId}");
            var recentPostsTask = forumClient.GetAsync($"/api/forum/posts/user/{userId}?page=1&pageSize=5");
            var favoritePostsTask = forumClient.GetAsync($"/api/forum/posts/favorites/{userId}?page=1&pageSize=5");
            var matchingStatsTask = matchingClient.GetAsync($"/api/matching/stats/{userId}");

            await Task.WhenAll(userTask, recentPostsTask, favoritePostsTask, matchingStatsTask);

            // 处理响应
            var userJson = userTask.Result.IsSuccessStatusCode 
                ? await userTask.Result.Content.ReadAsStringAsync() 
                : "{}";
            
            var recentPostsJson = recentPostsTask.Result.IsSuccessStatusCode 
                ? await recentPostsTask.Result.Content.ReadAsStringAsync() 
                : "{\"posts\":[],\"totalCount\":0}";
            
            var favoritePostsJson = favoritePostsTask.Result.IsSuccessStatusCode 
                ? await favoritePostsTask.Result.Content.ReadAsStringAsync() 
                : "{\"posts\":[],\"totalCount\":0}";
            
            var matchingStatsJson = matchingStatsTask.Result.IsSuccessStatusCode 
                ? await matchingStatsTask.Result.Content.ReadAsStringAsync() 
                : "{}";

            // 合并数据
            var result = new
            {
                user = JsonSerializer.Deserialize<JsonElement>(userJson),
                recentPosts = JsonSerializer.Deserialize<JsonElement>(recentPostsJson),
                favoritePosts = JsonSerializer.Deserialize<JsonElement>(favoritePostsJson),
                matchingStats = JsonSerializer.Deserialize<JsonElement>(matchingStatsJson),
                timestamp = DateTime.UtcNow
            };

            var resultJson = JsonSerializer.SerializeToDocument(result);
            
            // 缓存2分钟
            _cache.Set(cacheKey, resultJson, TimeSpan.FromMinutes(2));
            
            return resultJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate user dashboard for user {UserId}", userId);
            throw;
        }
    }

    public async Task<JsonDocument> GetForumStatsAsync()
    {
        var cacheKey = "forum_stats";
        
        if (_cache.TryGetValue(cacheKey, out JsonDocument? cachedResult))
        {
            return cachedResult!;
        }

        try
        {
            var forumClient = _httpClientFactory.CreateClient("ForumManager");

            // 并行获取论坛统计信息
            var totalPostsTask = forumClient.GetAsync("/api/forum/stats/posts");
            var totalCommentsTask = forumClient.GetAsync("/api/forum/stats/comments");
            var activeUsersTask = forumClient.GetAsync("/api/forum/stats/active-users");
            var popularPostsTask = forumClient.GetAsync("/api/forum/stats/popular-posts");

            await Task.WhenAll(totalPostsTask, totalCommentsTask, activeUsersTask, popularPostsTask);

            // 处理响应
            var totalPosts = totalPostsTask.Result.IsSuccessStatusCode 
                ? await totalPostsTask.Result.Content.ReadAsStringAsync() 
                : "0";
            
            var totalComments = totalCommentsTask.Result.IsSuccessStatusCode 
                ? await totalCommentsTask.Result.Content.ReadAsStringAsync() 
                : "0";
            
            var activeUsers = activeUsersTask.Result.IsSuccessStatusCode 
                ? await activeUsersTask.Result.Content.ReadAsStringAsync() 
                : "0";
            
            var popularPosts = popularPostsTask.Result.IsSuccessStatusCode 
                ? await popularPostsTask.Result.Content.ReadAsStringAsync() 
                : "[]";

            // 合并数据
            var result = new
            {
                totalPosts = JsonSerializer.Deserialize<JsonElement>(totalPosts),
                totalComments = JsonSerializer.Deserialize<JsonElement>(totalComments),
                activeUsers = JsonSerializer.Deserialize<JsonElement>(activeUsers),
                popularPosts = JsonSerializer.Deserialize<JsonElement>(popularPosts),
                timestamp = DateTime.UtcNow
            };

            var resultJson = JsonSerializer.SerializeToDocument(result);
            
            // 缓存10分钟
            _cache.Set(cacheKey, resultJson, TimeSpan.FromMinutes(10));
            
            return resultJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate forum stats");
            throw;
        }
    }
}
