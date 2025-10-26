using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Repositories
{
    /// <summary>
    /// 用户交互记录仓储实现
    /// </summary>
    public class UserInteractionRepository : IUserInteractionRepository
    {
        private readonly MatchingDbContext _context;

        public UserInteractionRepository(MatchingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserInteraction>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserInteractions
                .Where(ui => ui.UserId == userId)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserInteraction>> GetByUserIdAndTypeAsync(Guid userId, InteractionType type)
        {
            return await _context.UserInteractions
                .Where(ui => ui.UserId == userId && ui.Type == type)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserInteraction>> GetByUserPairAsync(Guid userId, Guid targetUserId)
        {
            return await _context.UserInteractions
                .Where(ui => ui.UserId == userId && ui.TargetUserId == targetUserId)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserInteraction> AddAsync(UserInteraction interaction)
        {
            _context.UserInteractions.Add(interaction);
            await _context.SaveChangesAsync();
            return interaction;
        }

        public async Task<UserInteraction> UpdateAsync(UserInteraction interaction)
        {
            _context.UserInteractions.Update(interaction);
            await _context.SaveChangesAsync();
            return interaction;
        }

        public async Task DeleteAsync(Guid id)
        {
            var interaction = await _context.UserInteractions.FindAsync(id);
            if (interaction != null)
            {
                _context.UserInteractions.Remove(interaction);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<UserInteraction>> GetRecentInteractionsAsync(Guid userId, DateTime since, int count = 100)
        {
            return await _context.UserInteractions
                .Where(ui => ui.UserId == userId && ui.CreatedAt >= since)
                .OrderByDescending(ui => ui.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, float>> GetUserRatingsAsync(Guid userId)
        {
            return await _context.UserInteractions
                .Where(ui => ui.UserId == userId && ui.Rating > 0)
                .GroupBy(ui => ui.TargetUserId)
                .ToDictionaryAsync(g => g.Key, g => g.Average(ui => ui.Rating));
        }

        /// <summary>
        /// 获取有评分行为的用户ID集合（至少对他人做过>0评分）
        /// </summary>
        public async Task<IEnumerable<Guid>> GetUsersWithRatingsAsync()
        {
            return await _context.UserInteractions
                .Where(ui => ui.Rating > 0)
                .Select(ui => ui.UserId)
                .Distinct()
                .ToListAsync();
        }

        // 已废弃：改用 GetUsersWithRatingsAsync()

        /// <summary>
        /// 分页获取用户交互记录
        /// </summary>
        public async Task<(IEnumerable<UserInteraction> Interactions, int TotalCount)> GetInteractionsWithPaginationAsync(
            Guid userId, 
            int page, 
            int pageSize, 
            InteractionType? type = null, 
            Guid? targetUserId = null)
        {
            var query = _context.UserInteractions
                .Where(ui => ui.UserId == userId);

            if (type.HasValue)
                query = query.Where(ui => ui.Type == type.Value);

            if (targetUserId.HasValue)
                query = query.Where(ui => ui.TargetUserId == targetUserId.Value);

            var totalCount = await query.CountAsync();

            var interactions = await query
                .OrderByDescending(ui => ui.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (interactions, totalCount);
        }

        /// <summary>
        /// 获取用户交互统计信息
        /// </summary>
        public async Task<Dictionary<InteractionType, int>> GetInteractionStatsAsync(Guid userId)
        {
            return await _context.UserInteractions
                .Where(ui => ui.UserId == userId)
                .GroupBy(ui => ui.Type)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取用户平均评分
        /// </summary>
        public async Task<float> GetUserAverageRatingAsync(Guid userId)
        {
            var avgRating = await _context.UserInteractions
                .Where(ui => ui.UserId == userId && ui.Rating > 0)
                .AverageAsync(ui => (double?)ui.Rating);

            return (float)(avgRating ?? 0.0);
        }

        /// <summary>
        /// 获取目标用户的平均评分
        /// </summary>
        public async Task<float> GetTargetUserAverageRatingAsync(Guid targetUserId)
        {
            var avgRating = await _context.UserInteractions
                .Where(ui => ui.TargetUserId == targetUserId && ui.Rating > 0)
                .AverageAsync(ui => (double?)ui.Rating);

            return (float)(avgRating ?? 0.0);
        }
    }
}
