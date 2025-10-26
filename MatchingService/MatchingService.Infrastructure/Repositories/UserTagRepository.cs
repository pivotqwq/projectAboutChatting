using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Repositories
{
    /// <summary>
    /// 用户标签关系仓储实现
    /// </summary>
    public class UserTagRepository : IUserTagRepository
    {
        private readonly MatchingDbContext _context;

        public UserTagRepository(MatchingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserTag>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserTags
                .Include(ut => ut.Tag)
                .Where(ut => ut.UserId == userId)
                .OrderByDescending(ut => ut.Weight)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserTag>> GetByTagIdAsync(Guid tagId)
        {
            return await _context.UserTags
                .Include(ut => ut.Tag)
                .Where(ut => ut.TagId == tagId)
                .OrderByDescending(ut => ut.Weight)
                .ToListAsync();
        }

        public async Task<UserTag?> GetByUserAndTagAsync(Guid userId, Guid tagId)
        {
            return await _context.UserTags
                .Include(ut => ut.Tag)
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TagId == tagId);
        }

        public async Task<IEnumerable<UserTag>> GetActiveUserTagsAsync(Guid userId)
        {
            return await _context.UserTags
                .Include(ut => ut.Tag)
                .Where(ut => ut.UserId == userId && ut.IsActive)
                .OrderByDescending(ut => ut.Weight)
                .ToListAsync();
        }

        public async Task<UserTag> AddAsync(UserTag userTag)
        {
            _context.UserTags.Add(userTag);
            await _context.SaveChangesAsync();
            return userTag;
        }

        public async Task<UserTag> UpdateAsync(UserTag userTag)
        {
            _context.UserTags.Update(userTag);
            await _context.SaveChangesAsync();
            return userTag;
        }

        public async Task DeleteAsync(Guid id)
        {
            var userTag = await _context.UserTags.FindAsync(id);
            if (userTag != null)
            {
                _context.UserTags.Remove(userTag);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByUserAndTagAsync(Guid userId, Guid tagId)
        {
            var userTag = await GetByUserAndTagAsync(userId, tagId);
            if (userTag != null)
            {
                _context.UserTags.Remove(userTag);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid tagId)
        {
            return await _context.UserTags
                .AnyAsync(ut => ut.UserId == userId && ut.TagId == tagId);
        }

        /// <summary>
        /// 根据分类获取用户标签
        /// </summary>
        public async Task<IEnumerable<UserTag>> GetUserTagsByCategoryAsync(Guid userId, TagCategory category)
        {
            return await _context.UserTags
                .Include(ut => ut.Tag)
                .Where(ut => ut.UserId == userId && ut.Tag.Category == category && ut.IsActive)
                .OrderByDescending(ut => ut.Weight)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户标签统计信息
        /// </summary>
        public async Task<Dictionary<TagCategory, int>> GetUserTagStatsAsync(Guid userId)
        {
            return await _context.UserTags
                .Include(ut => ut.Tag)
                .Where(ut => ut.UserId == userId && ut.IsActive)
                .GroupBy(ut => ut.Tag.Category)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }
    }
}
