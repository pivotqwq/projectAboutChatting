using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.IRepositories
{
    /// <summary>
    /// 用户交互记录仓储接口
    /// </summary>
    public interface IUserInteractionRepository
    {
        Task<IEnumerable<UserInteraction>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserInteraction>> GetByUserIdAndTypeAsync(Guid userId, InteractionType type);
        Task<IEnumerable<UserInteraction>> GetByUserPairAsync(Guid userId, Guid targetUserId);
        Task<UserInteraction> AddAsync(UserInteraction interaction);
        Task<UserInteraction> UpdateAsync(UserInteraction interaction);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<UserInteraction>> GetRecentInteractionsAsync(Guid userId, DateTime since, int count = 100);
        Task<Dictionary<Guid, float>> GetUserRatingsAsync(Guid userId);
        Task<IEnumerable<Guid>> GetUsersWithRatingsAsync();
    }
}
