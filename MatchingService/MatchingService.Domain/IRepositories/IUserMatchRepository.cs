using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.IRepositories
{
    /// <summary>
    /// 用户匹配记录仓储接口
    /// </summary>
    public interface IUserMatchRepository
    {
        Task<IEnumerable<UserMatch>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserMatch>> GetByUserIdAndStatusAsync(Guid userId, MatchStatus status);
        Task<UserMatch?> GetByUserPairAsync(Guid userId, Guid matchedUserId);
        Task<IEnumerable<UserMatch>> GetPendingMatchesAsync(Guid userId, int count = 20);
        Task<UserMatch> AddAsync(UserMatch userMatch);
        Task<UserMatch> UpdateAsync(UserMatch userMatch);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid userId, Guid matchedUserId);
        Task<IEnumerable<UserMatch>> GetRecentMatchesAsync(Guid userId, DateTime since, int count = 50);
    }
}
