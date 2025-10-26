using Microsoft.EntityFrameworkCore;
using UserManager.Domain;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture
{
    /// <summary>
    /// 用户个人信息仓储实现
    /// </summary>
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly UserDBContext _dbContext;

        public UserProfileRepository(UserDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task AddAsync(UserProfile userProfile)
        {
            _dbContext.UserProfiles.Add(userProfile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserProfile userProfile)
        {
            _dbContext.UserProfiles.Update(userProfile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            var profile = await _dbContext.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);
            
            if (profile != null)
            {
                _dbContext.UserProfiles.Remove(profile);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
