using MatchingService.Domain.Entities;

namespace MatchingService.Domain.IRepositories
{
    /// <summary>
    /// 用户标签关系仓储接口
    /// </summary>
    public interface IUserTagRepository
    {
        Task<IEnumerable<UserTag>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserTag>> GetByTagIdAsync(Guid tagId);
        Task<UserTag?> GetByUserAndTagAsync(Guid userId, Guid tagId);
        Task<IEnumerable<UserTag>> GetActiveUserTagsAsync(Guid userId);
        Task<UserTag> AddAsync(UserTag userTag);
        Task<UserTag> UpdateAsync(UserTag userTag);
        Task DeleteAsync(Guid id);
        Task DeleteByUserAndTagAsync(Guid userId, Guid tagId);
        Task<bool> ExistsAsync(Guid userId, Guid tagId);
    }
}
