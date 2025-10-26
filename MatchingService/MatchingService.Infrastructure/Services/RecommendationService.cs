using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;
using MatchType = MatchingService.Domain.ValueObjects.MatchType;

namespace MatchingService.Infrastructure.Services
{
    /// <summary>
    /// 推荐服务实现
    /// </summary>
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

        public async Task<IEnumerable<RecommendationResult>> GetTagBasedRecommendationsAsync(Guid userId, int count = 20)
        {
            // 获取用户的标签
            var userTags = await _userTagRepository.GetActiveUserTagsAsync(userId);
            if (!userTags.Any())
                return Enumerable.Empty<RecommendationResult>();

            var similarities = new Dictionary<Guid, float>();

            foreach (var userTag in userTags)
            {
                // 获取使用相同标签的其他用户
                var otherUserTags = await _userTagRepository.GetByTagIdAsync(userTag.TagId);
                
                foreach (var otherUserTag in otherUserTags.Where(ut => ut.UserId != userId && ut.IsActive))
                {
                    if (!similarities.ContainsKey(otherUserTag.UserId))
                        similarities[otherUserTag.UserId] = 0f;

                    // 计算标签相似度
                    var tagSimilarity = CalculateTagSimilarity(userTag, otherUserTag);
                    similarities[otherUserTag.UserId] += tagSimilarity;
                }
            }

            // 排序并返回推荐结果
            return similarities
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => new RecommendationResult(
                    kvp.Key,
                    kvp.Value,
                    MatchType.TagBased,
                    "基于标签相似度",
                    new Dictionary<string, object>
                    {
                        ["commonTags"] = userTags.Count(ut => similarities.ContainsKey(ut.UserId)),
                        ["similarityScore"] = kvp.Value
                    }));
        }

        public async Task<IEnumerable<RecommendationResult>> GetCollaborativeFilteringRecommendationsAsync(Guid userId, int count = 20)
        {
            // 获取用户的评分历史
            var userRatings = await _userInteractionRepository.GetUserRatingsAsync(userId);
            if (!userRatings.Any())
                return Enumerable.Empty<RecommendationResult>();

            // 获取有评分行为的用户（更合理的候选集）
            var allUsers = await _userInteractionRepository.GetUsersWithRatingsAsync();
            var similarUsers = new Dictionary<Guid, float>();

            // 计算与其他用户的相似度
            foreach (var otherUserId in allUsers.Where(id => id != userId))
            {
                var similarity = await CalculateUserSimilarityAsync(userId, otherUserId);
                if (similarity > 0.1f) // 相似度阈值
                {
                    similarUsers[otherUserId] = similarity;
                }
            }

            // 基于相似用户推荐
            var recommendations = new Dictionary<Guid, float>();
            foreach (var similarUser in similarUsers.OrderByDescending(kvp => kvp.Value).Take(50))
            {
                var similarUserRatings = await _userInteractionRepository.GetUserRatingsAsync(similarUser.Key);
                
                foreach (var rating in similarUserRatings)
                {
                    if (!userRatings.ContainsKey(rating.Key)) // 用户还未交互过
                    {
                        if (!recommendations.ContainsKey(rating.Key))
                            recommendations[rating.Key] = 0f;
                        
                        recommendations[rating.Key] += similarUser.Value * rating.Value;
                    }
                }
            }

            return recommendations
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => new RecommendationResult(
                    kvp.Key,
                    kvp.Value / 5.0f, // 归一化到0-1
                    MatchType.CollaborativeFiltering,
                    "协同过滤推荐",
                    new Dictionary<string, object>
                    {
                        ["similarUsers"] = similarUsers.Count,
                        ["predictedRating"] = kvp.Value
                    }));
        }

        public async Task<IEnumerable<RecommendationResult>> GetLocationBasedRecommendationsAsync(Guid userId, Location userLocation, double maxDistanceKm = 50, int count = 20)
        {
            var recentInteractions = await _userInteractionRepository.GetRecentInteractionsAsync(userId, DateTime.UtcNow.AddDays(-30), 100);
            
            var locationRecommendations = new Dictionary<Guid, float>();
            
            foreach (var interaction in recentInteractions)
            {
                // 这里应该根据实际位置数据计算距离
                // 简化实现：基于交互频率和类型
                var score = CalculateLocationScore(interaction);
                if (score > 0.3f)
                {
                    locationRecommendations[interaction.TargetUserId] = score;
                }
            }

            return locationRecommendations
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => new RecommendationResult(
                    kvp.Key,
                    kvp.Value,
                    MatchType.LocationBased,
                    "基于地理位置",
                    new Dictionary<string, object>
                    {
                        ["distance"] = "N/A", // 实际实现中计算真实距离
                        ["city"] = userLocation.City ?? "Unknown",
                        ["province"] = userLocation.Province ?? "Unknown"
                    }));
        }

        public async Task<IEnumerable<RecommendationResult>> GetHybridRecommendationsAsync(Guid userId, Location? userLocation = null, int count = 20)
        {
            var allRecommendations = new List<RecommendationResult>();
            
            // 1. 基于标签的推荐 (权重: 40%)
            var tagBasedRecs = await GetTagBasedRecommendationsAsync(userId, count);
            allRecommendations.AddRange(tagBasedRecs.Select(r => new RecommendationResult(
                r.UserId, r.Score * 0.4f, r.MatchType, r.Reason, r.Metadata)));

            // 2. 协同过滤推荐 (权重: 30%)
            var cfRecs = await GetCollaborativeFilteringRecommendationsAsync(userId, count);
            allRecommendations.AddRange(cfRecs.Select(r => new RecommendationResult(
                r.UserId, r.Score * 0.3f, r.MatchType, r.Reason, r.Metadata)));

            // 3. 地理位置推荐 (权重: 20%)
            if (userLocation != null)
            {
                var locationRecs = await GetLocationBasedRecommendationsAsync(userId, userLocation, 50, count);
                allRecommendations.AddRange(locationRecs.Select(r => new RecommendationResult(
                    r.UserId, r.Score * 0.2f, r.MatchType, r.Reason, r.Metadata)));
            }

            // 4. 随机推荐 (权重: 10%)
            var randomRecs = await GetRandomRecommendationsAsync(userId, count / 4);
            allRecommendations.AddRange(randomRecs.Select(r => new RecommendationResult(
                r.UserId, r.Score * 0.1f, r.MatchType, r.Reason, r.Metadata)));

            // 5. 合并并排序推荐结果
            var mergedRecs = allRecommendations
                .GroupBy(r => r.UserId)
                .Select(g => new RecommendationResult(
                    g.Key,
                    g.Sum(r => r.Score),
                    MatchType.Hybrid,
                    "混合推荐算法",
                    g.First().Metadata))
                .OrderByDescending(r => r.Score)
                .Take(count);

            return mergedRecs;
        }

        public async Task<float> CalculateUserSimilarityAsync(Guid userId1, Guid userId2)
        {
            var user1Ratings = await _userInteractionRepository.GetUserRatingsAsync(userId1);
            var user2Ratings = await _userInteractionRepository.GetUserRatingsAsync(userId2);

            if (!user1Ratings.Any() || !user2Ratings.Any())
                return 0f;

            // 找到共同评分的用户
            var commonUsers = user1Ratings.Keys.Intersect(user2Ratings.Keys).ToList();
            if (commonUsers.Count < 2)
                return 0f;

            // 计算皮尔逊相关系数
            var user1CommonRatings = commonUsers.Select(u => user1Ratings[u]).ToList();
            var user2CommonRatings = commonUsers.Select(u => user2Ratings[u]).ToList();

            return CalculatePearsonCorrelation(user1CommonRatings, user2CommonRatings);
        }

        public async Task UpdateUserPreferenceModelAsync(Guid userId)
        {
            // 更新用户偏好模型
            // 这里可以实现更复杂的机器学习模型更新逻辑
            // 目前简化实现
            await Task.CompletedTask;
        }

        #region 私有方法

        private float CalculateTagSimilarity(UserTag userTag1, UserTag userTag2)
        {
            // 计算标签相似度
            var weightSimilarity = Math.Min(userTag1.Weight, userTag2.Weight) / Math.Max(userTag1.Weight, userTag2.Weight);
            return weightSimilarity * 0.8f; // 基础相似度
        }

        private float CalculateLocationScore(UserInteraction interaction)
        {
            // 基于交互类型计算位置分数
            return interaction.Type switch
            {
                InteractionType.Like => 0.8f,
                InteractionType.Follow => 0.9f,
                InteractionType.SendMessage => 0.7f,
                InteractionType.ViewProfile => 0.3f,
                _ => 0.5f
            };
        }

        private float CalculatePearsonCorrelation(List<float> x, List<float> y)
        {
            if (x.Count != y.Count || x.Count < 2)
                return 0f;

            var n = x.Count;
            var sumX = x.Sum();
            var sumY = y.Sum();
            var sumXY = x.Zip(y, (a, b) => a * b).Sum();
            var sumX2 = x.Sum(a => a * a);
            var sumY2 = y.Sum(a => a * a);

            var numerator = n * sumXY - sumX * sumY;
            var denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            if (Math.Abs(denominator) < 1e-10)
                return 0f;

            return (float)(numerator / denominator);
        }

        private async Task<IEnumerable<RecommendationResult>> GetRandomRecommendationsAsync(Guid userId, int count)
        {
            // 获取随机推荐
            var allUsers = await _userInteractionRepository.GetUsersWithRatingsAsync();
            var randomUsers = allUsers
                .Where(id => id != userId)
                .OrderBy(x => Guid.NewGuid())
                .Take(count);

            return randomUsers.Select(userId => new RecommendationResult(
                userId,
                0.1f, // 随机推荐的基础分数
                MatchType.Random,
                "随机推荐",
                new Dictionary<string, object> { ["random"] = true }));
        }

        #endregion
    }
}
