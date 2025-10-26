using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.Services
{
    /// <summary>
    /// 推荐服务接口
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// 基于标签的推荐
        /// </summary>
        Task<IEnumerable<RecommendationResult>> GetTagBasedRecommendationsAsync(Guid userId, int count = 20);

        /// <summary>
        /// 基于协同过滤的推荐
        /// </summary>
        Task<IEnumerable<RecommendationResult>> GetCollaborativeFilteringRecommendationsAsync(Guid userId, int count = 20);

        /// <summary>
        /// 基于地理位置的推荐
        /// </summary>
        Task<IEnumerable<RecommendationResult>> GetLocationBasedRecommendationsAsync(Guid userId, Location userLocation, double maxDistanceKm = 50, int count = 20);

        /// <summary>
        /// 混合推荐算法
        /// </summary>
        Task<IEnumerable<RecommendationResult>> GetHybridRecommendationsAsync(Guid userId, Location? userLocation = null, int count = 20);

        /// <summary>
        /// 计算用户相似度
        /// </summary>
        Task<float> CalculateUserSimilarityAsync(Guid userId1, Guid userId2);

        /// <summary>
        /// 更新用户偏好模型
        /// </summary>
        Task UpdateUserPreferenceModelAsync(Guid userId);
    }
}
