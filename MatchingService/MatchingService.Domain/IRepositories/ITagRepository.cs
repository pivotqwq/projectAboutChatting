using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.IRepositories
{
    /// <summary>
    /// 标签仓储接口
    /// </summary>
    public interface ITagRepository
    {
        Task<Tag?> GetByIdAsync(Guid id);
        Task<Tag?> GetByNameAsync(string name);
        Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category);
        Task<IEnumerable<Tag>> GetPopularTagsAsync(int count = 20);
        Task<IEnumerable<Tag>> SearchTagsAsync(string keyword, int count = 10);
        Task<Tag> AddAsync(Tag tag);
        Task<Tag> UpdateAsync(Tag tag);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string name);
    }
}
