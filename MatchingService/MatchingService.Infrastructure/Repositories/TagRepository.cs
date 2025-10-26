using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using MatchingService.Domain.IRepositories;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Repositories
{
    /// <summary>
    /// 标签仓储实现
    /// </summary>
    public class TagRepository : ITagRepository
    {
        private readonly MatchingDbContext _context;

        public TagRepository(MatchingDbContext context)
        {
            _context = context;
        }

        public async Task<Tag?> GetByIdAsync(Guid id)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category)
        {
            return await _context.Tags
                .Where(t => t.Category == category && t.IsActive)
                .OrderByDescending(t => t.UsageCount)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count = 20)
        {
            return await _context.Tags
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UsageCount)
                .ThenByDescending(t => t.LastUsedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> SearchTagsAsync(string keyword, int count = 10)
        {
            var lowerKeyword = keyword.ToLower();
            return await _context.Tags
                .Where(t => t.IsActive && (t.Name.ToLower().Contains(lowerKeyword) || t.Description.ToLower().Contains(lowerKeyword)))
                .OrderByDescending(t => t.UsageCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Tag> AddAsync(Tag tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<Tag> UpdateAsync(Tag tag)
        {
            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task DeleteAsync(Guid id)
        {
            var tag = await GetByIdAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Tags.AnyAsync(t => t.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Tags.AnyAsync(t => t.Name == name);
        }
    }
}
