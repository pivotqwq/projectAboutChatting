using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ForumManager.Infrastructure
{
    /// <summary>
    /// Redis论坛缓存服务
    /// </summary>
    public class RedisForumCache
    {
        private readonly IDistributedCache _cache;

        public RedisForumCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// 缓存热门帖子
        /// </summary>
        public async Task CacheHotPostsAsync(string key, object posts, TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(posts);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        /// <summary>
        /// 获取缓存的热门帖子
        /// </summary>
        public async Task<T?> GetHotPostsAsync<T>(string key)
        {
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// 缓存帖子详情
        /// </summary>
        public async Task CachePostAsync(string key, object post, TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(post);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        /// <summary>
        /// 获取缓存的帖子详情
        /// </summary>
        public async Task<T?> GetPostAsync<T>(string key)
        {
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// 清除帖子缓存
        /// </summary>
        public async Task RemovePostCacheAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// 清除热门帖子缓存
        /// </summary>
        public async Task RemoveHotPostsCacheAsync()
        {
            // 这里可以根据实际需要清除所有热门帖子相关的缓存
            // 由于Redis没有通配符删除，这里提供一个基础实现
            // 实际项目中可能需要维护一个缓存键的集合
        }
    }
}
