using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.ValueObjects;
using MatchType = MatchingService.Domain.ValueObjects.MatchType;

namespace MatchingService.Infrastructure.Repositories
{
    /// <summary>
    /// 用户匹配记录仓储实现
    /// </summary>
    public class UserMatchRepository : IUserMatchRepository
    {
        private readonly MatchingDbContext _context;

        public UserMatchRepository(MatchingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserMatch>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserMatches
                .Where(um => um.UserId == userId)
                .OrderByDescending(um => um.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserMatch>> GetByUserIdAndStatusAsync(Guid userId, MatchStatus status)
        {
            return await _context.UserMatches
                .Where(um => um.UserId == userId && um.Status == status)
                .OrderByDescending(um => um.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserMatch?> GetByUserPairAsync(Guid userId, Guid matchedUserId)
        {
            return await _context.UserMatches
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MatchedUserId == matchedUserId);
        }

        public async Task<IEnumerable<UserMatch>> GetPendingMatchesAsync(Guid userId, int count = 20)
        {
            return await _context.UserMatches
                .Where(um => um.UserId == userId && um.Status == MatchStatus.Pending)
                .OrderByDescending(um => um.MatchScore)
                .Take(count)
                .ToListAsync();
        }

        public async Task<UserMatch> AddAsync(UserMatch userMatch)
        {
            _context.UserMatches.Add(userMatch);
            await _context.SaveChangesAsync();
            return userMatch;
        }

        public async Task<UserMatch> UpdateAsync(UserMatch userMatch)
        {
            _context.UserMatches.Update(userMatch);
            await _context.SaveChangesAsync();
            return userMatch;
        }

        public async Task DeleteAsync(Guid id)
        {
            var userMatch = await _context.UserMatches.FindAsync(id);
            if (userMatch != null)
            {
                _context.UserMatches.Remove(userMatch);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid matchedUserId)
        {
            return await _context.UserMatches
                .AnyAsync(um => um.UserId == userId && um.MatchedUserId == matchedUserId);
        }

        public async Task<IEnumerable<UserMatch>> GetRecentMatchesAsync(Guid userId, DateTime since, int count = 50)
        {
            return await _context.UserMatches
                .Where(um => um.UserId == userId && um.CreatedAt >= since)
                .OrderByDescending(um => um.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// 分页获取用户匹配记录
        /// </summary>
        public async Task<(IEnumerable<UserMatch> Matches, int TotalCount)> GetMatchesWithPaginationAsync(
            Guid userId, 
            int page, 
            int pageSize, 
            MatchStatus? status = null, 
            MatchType? matchType = null)
        {
            var query = _context.UserMatches
                .Where(um => um.UserId == userId);

            if (status.HasValue)
                query = query.Where(um => um.Status == status.Value);

            if (matchType.HasValue)
                query = query.Where(um => um.MatchType == matchType.Value);

            var totalCount = await query.CountAsync();

            var matches = await query
                .OrderByDescending(um => um.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (matches, totalCount);
        }

        /// <summary>
        /// 获取匹配统计信息
        /// </summary>
        public async Task<Dictionary<MatchStatus, int>> GetMatchStatsAsync(Guid userId)
        {
            return await _context.UserMatches
                .Where(um => um.UserId == userId)
                .GroupBy(um => um.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取高匹配分数的记录
        /// </summary>
        public async Task<IEnumerable<UserMatch>> GetHighScoreMatchesAsync(Guid userId, float minScore = 0.7f, int count = 10)
        {
            return await _context.UserMatches
                .Where(um => um.UserId == userId && um.MatchScore >= minScore)
                .OrderByDescending(um => um.MatchScore)
                .Take(count)
                .ToListAsync();
        }
    }
}
