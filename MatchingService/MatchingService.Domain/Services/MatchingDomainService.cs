using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.ValueObjects;
using MediatR;
using MatchType = MatchingService.Domain.ValueObjects.MatchType;

namespace MatchingService.Domain.Services
{
    /// <summary>
    /// 匹配领域服务
    /// </summary>
    public class MatchingDomainService
    {
        private readonly ITagRepository _tagRepository;
        private readonly IUserTagRepository _userTagRepository;
        private readonly IUserMatchRepository _userMatchRepository;
        private readonly IUserInteractionRepository _userInteractionRepository;
        private readonly IRecommendationService _recommendationService;
        private readonly ICacheService _cacheService;
        private readonly IMediator _mediator;

        public MatchingDomainService(
            ITagRepository tagRepository,
            IUserTagRepository userTagRepository,
            IUserMatchRepository userMatchRepository,
            IUserInteractionRepository userInteractionRepository,
            IRecommendationService recommendationService,
            ICacheService cacheService,
            IMediator mediator)
        {
            _tagRepository = tagRepository;
            _userTagRepository = userTagRepository;
            _userMatchRepository = userMatchRepository;
            _userInteractionRepository = userInteractionRepository;
            _recommendationService = recommendationService;
            _cacheService = cacheService;
            _mediator = mediator;
        }

        /// <summary>
        /// 为用户添加标签
        /// </summary>
        public async Task<UserTag> AddUserTagAsync(Guid userId, Guid tagId, float weight = 1.0f)
        {
            // 检查标签是否存在
            var tag = await _tagRepository.GetByIdAsync(tagId);
            if (tag == null)
            {
                throw new ArgumentException("标签不存在");
            }

            // 检查用户是否已有该标签
            var existingUserTag = await _userTagRepository.GetByUserAndTagAsync(userId, tagId);
            if (existingUserTag != null)
            {
                existingUserTag.UpdateWeight(weight);
                return await _userTagRepository.UpdateAsync(existingUserTag);
            }

            // 创建新的用户标签关系
            var userTag = new UserTag(userId, tagId, weight);
            var result = await _userTagRepository.AddAsync(userTag);

            // 增加标签使用次数
            tag.IncrementUsage();
            await _tagRepository.UpdateAsync(tag);

            // 清除用户标签缓存
            await _cacheService.ClearUserTagsCacheAsync(userId);

            return result;
        }

        /// <summary>
        /// 移除用户标签
        /// </summary>
        public async Task RemoveUserTagAsync(Guid userId, Guid tagId)
        {
            var userTag = await _userTagRepository.GetByUserAndTagAsync(userId, tagId);
            if (userTag != null)
            {
                await _userTagRepository.DeleteByUserAndTagAsync(userId, tagId);

                // 减少标签使用次数
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag != null)
                {
                    tag.DecrementUsage();
                    await _tagRepository.UpdateAsync(tag);
                }

                // 清除用户标签缓存
                await _cacheService.ClearUserTagsCacheAsync(userId);
            }
        }

        /// <summary>
        /// 记录用户交互
        /// </summary>
        public async Task RecordUserInteractionAsync(Guid userId, Guid targetUserId, InteractionType type, float rating = 0.0f, string? context = null)
        {
            var interaction = new UserInteraction(userId, targetUserId, type, rating);
            if (!string.IsNullOrEmpty(context))
            {
                interaction.AddContext(context);
            }

            await _userInteractionRepository.AddAsync(interaction);

            // 更新用户偏好模型
            await _recommendationService.UpdateUserPreferenceModelAsync(userId);

            // 清除推荐缓存
            await _cacheService.ClearUserRecommendationsCacheAsync(userId);
        }

        /// <summary>
        /// 处理匹配结果
        /// </summary>
        public async Task<UserMatch> ProcessMatchAsync(Guid userId, Guid matchedUserId, float matchScore, MatchType matchType)
        {
            // 检查是否已存在匹配记录
            var existingMatch = await _userMatchRepository.GetByUserPairAsync(userId, matchedUserId);
            if (existingMatch != null)
            {
                existingMatch.UpdateMatchScore(matchScore);
                return await _userMatchRepository.UpdateAsync(existingMatch);
            }

            // 创建新的匹配记录
            var userMatch = new UserMatch(userId, matchedUserId, matchScore, matchType);
            return await _userMatchRepository.AddAsync(userMatch);
        }

        /// <summary>
        /// 更新匹配状态
        /// </summary>
        public async Task UpdateMatchStatusAsync(Guid userId, Guid matchedUserId, MatchStatus status)
        {
            var match = await _userMatchRepository.GetByUserPairAsync(userId, matchedUserId);
            if (match != null)
            {
                match.UpdateStatus(status);
                await _userMatchRepository.UpdateAsync(match);

                // 如果接受了匹配，记录交互
                if (status == MatchStatus.Accepted)
                {
                    await RecordUserInteractionAsync(userId, matchedUserId, InteractionType.Like, 5.0f, "接受匹配");
                }
            }
        }

        /// <summary>
        /// 获取用户推荐列表
        /// </summary>
        public async Task<IEnumerable<RecommendationResult>> GetUserRecommendationsAsync(Guid userId, Location? userLocation = null, int count = 20)
        {
            // 先尝试从缓存获取
            var cachedRecommendations = await _cacheService.GetCachedUserRecommendationsAsync(userId);
            if (cachedRecommendations != null)
            {
                return cachedRecommendations.Take(count);
            }

            // 使用混合推荐算法
            var recommendations = await _recommendationService.GetHybridRecommendationsAsync(userId, userLocation, count);

            // 缓存推荐结果（5分钟）
            await _cacheService.CacheUserRecommendationsAsync(userId, recommendations, TimeSpan.FromMinutes(5));

            return recommendations;
        }

        /// <summary>
        /// 获取热门标签
        /// </summary>
        public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count = 20)
        {
            // 先尝试从缓存获取
            var cachedTags = await _cacheService.GetCachedPopularTagsAsync();
            if (cachedTags != null)
            {
                return cachedTags.Take(count);
            }

            // 从数据库获取
            var tags = await _tagRepository.GetPopularTagsAsync(count);

            // 缓存结果（10分钟）
            await _cacheService.CachePopularTagsAsync(tags, TimeSpan.FromMinutes(10));

            return tags;
        }

        /// <summary>
        /// 搜索标签
        /// </summary>
        public async Task<IEnumerable<Tag>> SearchTagsAsync(string keyword, int count = 10)
        {
            return await _tagRepository.SearchTagsAsync(keyword, count);
        }

        /// <summary>
        /// 创建新标签
        /// </summary>
        public async Task<Tag> CreateTagAsync(string name, string description, TagCategory category)
        {
            // 检查标签名是否已存在
            var existingTag = await _tagRepository.GetByNameAsync(name);
            if (existingTag != null)
            {
                throw new ArgumentException("标签名已存在");
            }

            var tag = new Tag(name, description, category);
            return await _tagRepository.AddAsync(tag);
        }

        /// <summary>
        /// 根据分类获取标签
        /// </summary>
        public async Task<IEnumerable<Tag>> GetTagsByCategoryAsync(TagCategory category)
        {
            return await _tagRepository.GetByCategoryAsync(category);
        }

        /// <summary>
        /// 根据ID获取标签
        /// </summary>
        public async Task<Tag?> GetTagByIdAsync(Guid id)
        {
            return await _tagRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        public async Task<Tag?> UpdateTagAsync(Guid id, string name, string description, TagCategory category)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                return null;
            }

            // 检查新名称是否与其他标签冲突
            var existingTag = await _tagRepository.GetByNameAsync(name);
            if (existingTag != null && existingTag.Id != id)
            {
                throw new ArgumentException("标签名已存在");
            }

            tag.UpdateInfo(name, description, category);
            return await _tagRepository.UpdateAsync(tag);
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        public async Task<bool> DeleteTagAsync(Guid id)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                return false;
            }

            await _tagRepository.DeleteAsync(id);
            return true;
        }

        /// <summary>
        /// 获取用户标签列表
        /// </summary>
        public async Task<IEnumerable<UserTag>> GetUserTagsAsync(Guid userId)
        {
            return await _userTagRepository.GetByUserIdAsync(userId);
        }

        /// <summary>
        /// 获取单个用户标签
        /// </summary>
        public async Task<UserTag?> GetUserTagAsync(Guid userId, Guid tagId)
        {
            return await _userTagRepository.GetByUserAndTagAsync(userId, tagId);
        }

        /// <summary>
        /// 更新用户标签权重
        /// </summary>
        public async Task<UserTag?> UpdateUserTagAsync(Guid userId, Guid tagId, float weight)
        {
            var userTag = await _userTagRepository.GetByUserAndTagAsync(userId, tagId);
            if (userTag == null)
            {
                return null;
            }

            userTag.UpdateWeight(weight);
            return await _userTagRepository.UpdateAsync(userTag);
        }

        /// <summary>
        /// 获取基于标签的推荐
        /// </summary>
        public async Task<IEnumerable<RecommendationResult>> GetTagBasedRecommendationsAsync(Guid userId, int count = 20)
        {
            return await _recommendationService.GetTagBasedRecommendationsAsync(userId, count);
        }

        /// <summary>
        /// 获取协同过滤推荐
        /// </summary>
        public async Task<IEnumerable<RecommendationResult>> GetCollaborativeFilteringRecommendationsAsync(Guid userId, int count = 20)
        {
            return await _recommendationService.GetCollaborativeFilteringRecommendationsAsync(userId, count);
        }

        /// <summary>
        /// 获取基于地理位置的推荐
        /// </summary>
        public async Task<IEnumerable<RecommendationResult>> GetLocationBasedRecommendationsAsync(Guid userId, Location userLocation, double maxDistanceKm = 50, int count = 20)
        {
            return await _recommendationService.GetLocationBasedRecommendationsAsync(userId, userLocation, maxDistanceKm, count);
        }
    }
}
