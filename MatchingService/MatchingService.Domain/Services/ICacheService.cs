using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.Services
{
    /// <summary>
    /// 缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 缓存热门标签
        /// </summary>
        Task CachePopularTagsAsync(IEnumerable<Tag> tags, TimeSpan expiration);

        /// <summary>
        /// 获取缓存的热门标签
        /// </summary>
        Task<IEnumerable<Tag>?> GetCachedPopularTagsAsync();

        /// <summary>
        /// 缓存用户推荐结果
        /// </summary>
        Task CacheUserRecommendationsAsync(Guid userId, IEnumerable<RecommendationResult> recommendations, TimeSpan expiration);

        /// <summary>
        /// 获取缓存的用户推荐结果
        /// </summary>
        Task<IEnumerable<RecommendationResult>?> GetCachedUserRecommendationsAsync(Guid userId);

        /// <summary>
        /// 清除用户推荐缓存
        /// </summary>
        Task ClearUserRecommendationsCacheAsync(Guid userId);

        /// <summary>
        /// 缓存用户标签
        /// </summary>
        Task CacheUserTagsAsync(Guid userId, IEnumerable<UserTag> userTags, TimeSpan expiration);

        /// <summary>
        /// 获取缓存的用户标签
        /// </summary>
        Task<IEnumerable<UserTag>?> GetCachedUserTagsAsync(Guid userId);

        /// <summary>
        /// 清除用户标签缓存
        /// </summary>
        Task ClearUserTagsCacheAsync(Guid userId);
    }
}
