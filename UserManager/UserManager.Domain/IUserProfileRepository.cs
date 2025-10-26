using UserManager.Domain.Entities;

namespace UserManager.Domain
{
    /// <summary>
    /// 用户个人信息仓储接口
    /// </summary>
    public interface IUserProfileRepository
    {
        /// <summary>
        /// 根据用户ID获取个人信息
        /// </summary>
        Task<UserProfile?> GetByUserIdAsync(Guid userId);
        
        /// <summary>
        /// 添加个人信息
        /// </summary>
        Task AddAsync(UserProfile userProfile);
        
        /// <summary>
        /// 更新个人信息
        /// </summary>
        Task UpdateAsync(UserProfile userProfile);
        
        /// <summary>
        /// 根据用户ID删除个人信息
        /// </summary>
        Task DeleteByUserIdAsync(Guid userId);
    }
}
