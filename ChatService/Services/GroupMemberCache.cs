using StackExchange.Redis;
using System.Text.Json;

namespace ChatService.Services
{
    /// <summary>
    /// 群成员缓存服务 - 使用 Redis 缓存群成员信息，减少对 UserManager API 的调用
    /// </summary>
    public class GroupMemberCache
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<GroupMemberCache> _logger;
        private const string MemberSetPrefix = "chat:group-members:";  // set of userIds per group
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5); // 缓存5分钟

        public GroupMemberCache(IConnectionMultiplexer redis, ILogger<GroupMemberCache> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// 检查用户是否是群组成员（先从缓存查找，缓存未命中则返回null）
        /// </summary>
        public async Task<bool?> IsMemberFromCacheAsync(string groupId, string userId)
        {
            try
            {
                if (!Guid.TryParse(groupId, out var gid) || !Guid.TryParse(userId, out var uid))
                {
                    return null;
                }

                var db = _redis.GetDatabase();
                var cacheKey = MemberSetPrefix + gid;
                
                // 检查缓存是否存在
                var exists = await db.KeyExistsAsync(cacheKey);
                if (!exists)
                {
                    return null; // 缓存未命中
                }

                // 从缓存中检查用户是否在成员列表中
                var isMember = await db.SetContainsAsync(cacheKey, uid.ToString());
                return isMember;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从缓存检查群成员失败: GroupId={GroupId}, UserId={UserId}", groupId, userId);
                return null; // 缓存失败，返回null让调用方调用API
            }
        }

        /// <summary>
        /// 缓存群成员列表
        /// </summary>
        public async Task CacheGroupMembersAsync(string groupId, List<Guid> memberIds)
        {
            try
            {
                if (!Guid.TryParse(groupId, out var gid))
                {
                    return;
                }

                var db = _redis.GetDatabase();
                var cacheKey = MemberSetPrefix + gid;
                
                // 删除旧缓存
                await db.KeyDeleteAsync(cacheKey);
                
                // 如果有成员，添加到缓存
                if (memberIds != null && memberIds.Count > 0)
                {
                    var memberStrings = memberIds.Select(id => (RedisValue)id.ToString()).ToArray();
                    await db.SetAddAsync(cacheKey, memberStrings);
                    await db.KeyExpireAsync(cacheKey, CacheExpiration);
                    _logger.LogDebug("已缓存群组 {GroupId} 的 {Count} 个成员", groupId, memberIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "缓存群成员列表失败: GroupId={GroupId}", groupId);
            }
        }

        /// <summary>
        /// 清除群组成员缓存（当成员变更时调用）
        /// </summary>
        public async Task InvalidateCacheAsync(string groupId)
        {
            try
            {
                if (!Guid.TryParse(groupId, out var gid))
                {
                    return;
                }

                var db = _redis.GetDatabase();
                var cacheKey = MemberSetPrefix + gid;
                await db.KeyDeleteAsync(cacheKey);
                _logger.LogDebug("已清除群组 {GroupId} 的成员缓存", groupId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除群成员缓存失败: GroupId={GroupId}", groupId);
            }
        }
    }
}

