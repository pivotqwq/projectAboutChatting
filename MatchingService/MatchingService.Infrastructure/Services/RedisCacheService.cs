using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MatchingService.Domain.Entities;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Services
{
    /// <summary>
    /// Redis缓存服务实现
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task CachePopularTagsAsync(IEnumerable<Tag> tags, TimeSpan expiration)
        {
            var key = "popular_tags";
            var json = JsonSerializer.Serialize(tags, _jsonOptions);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        public async Task<IEnumerable<Tag>?> GetCachedPopularTagsAsync()
        {
            var key = "popular_tags";
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<IEnumerable<Tag>>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                // 如果反序列化失败，清除缓存
                await _cache.RemoveAsync(key);
                return null;
            }
        }

        public async Task CacheUserRecommendationsAsync(Guid userId, IEnumerable<RecommendationResult> recommendations, TimeSpan expiration)
        {
            var key = $"recommendations:{userId}";
            var json = JsonSerializer.Serialize(recommendations, _jsonOptions);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        public async Task<IEnumerable<RecommendationResult>?> GetCachedUserRecommendationsAsync(Guid userId)
        {
            var key = $"recommendations:{userId}";
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<IEnumerable<RecommendationResult>>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                // 如果反序列化失败，清除缓存
                await _cache.RemoveAsync(key);
                return null;
            }
        }

        public async Task ClearUserRecommendationsCacheAsync(Guid userId)
        {
            var key = $"recommendations:{userId}";
            await _cache.RemoveAsync(key);
        }

        public async Task CacheUserTagsAsync(Guid userId, IEnumerable<UserTag> userTags, TimeSpan expiration)
        {
            var key = $"user_tags:{userId}";
            var json = JsonSerializer.Serialize(userTags, _jsonOptions);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        public async Task<IEnumerable<UserTag>?> GetCachedUserTagsAsync(Guid userId)
        {
            var key = $"user_tags:{userId}";
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<IEnumerable<UserTag>>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                // 如果反序列化失败，清除缓存
                await _cache.RemoveAsync(key);
                return null;
            }
        }

        public async Task ClearUserTagsCacheAsync(Guid userId)
        {
            var key = $"user_tags:{userId}";
            await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// 缓存用户匹配记录
        /// </summary>
        public async Task CacheUserMatchesAsync(Guid userId, IEnumerable<UserMatch> matches, TimeSpan expiration)
        {
            var key = $"user_matches:{userId}";
            var json = JsonSerializer.Serialize(matches, _jsonOptions);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        /// <summary>
        /// 获取缓存的用户匹配记录
        /// </summary>
        public async Task<IEnumerable<UserMatch>?> GetCachedUserMatchesAsync(Guid userId)
        {
            var key = $"user_matches:{userId}";
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<IEnumerable<UserMatch>>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                await _cache.RemoveAsync(key);
                return null;
            }
        }

        /// <summary>
        /// 清除用户匹配缓存
        /// </summary>
        public async Task ClearUserMatchesCacheAsync(Guid userId)
        {
            var key = $"user_matches:{userId}";
            await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// 清除所有用户相关缓存
        /// </summary>
        public async Task ClearAllUserCacheAsync(Guid userId)
        {
            await Task.WhenAll(
                ClearUserRecommendationsCacheAsync(userId),
                ClearUserTagsCacheAsync(userId),
                ClearUserMatchesCacheAsync(userId)
            );
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                await _cache.RemoveAsync(key);
                return default;
            }
        }
    }
}
