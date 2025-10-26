using StackExchange.Redis;

namespace ChatService.Services
{
    public class PresenceService
    {
        private readonly IConnectionMultiplexer _redis;
        private const string OnlineUsersKey = "chat:online-users"; // set of userIds
        private const string UserConnPrefix = "chat:user-conns:"; // set of connectionIds per user

        public PresenceService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task AddConnectionAsync(string userId, string connectionId)
        {
            var db = _redis.GetDatabase();
            await db.SetAddAsync(OnlineUsersKey, userId);
            await db.SetAddAsync(UserConnPrefix + userId, connectionId);
        }

        public async Task<bool> RemoveConnectionAsync(string userId, string connectionId)
        {
            var db = _redis.GetDatabase();
            await db.SetRemoveAsync(UserConnPrefix + userId, connectionId);
            var remaining = await db.SetLengthAsync(UserConnPrefix + userId);
            if (remaining == 0)
            {
                await db.SetRemoveAsync(OnlineUsersKey, userId);
                return true; // no more connections for this user
            }
            return false;
        }

        public async Task<bool> IsOnlineAsync(string userId)
        {
            var db = _redis.GetDatabase();
            return await db.SetContainsAsync(OnlineUsersKey, userId);
        }

        public async Task<string[]> GetOnlineUsersAsync()
        {
            var db = _redis.GetDatabase();
            var members = await db.SetMembersAsync(OnlineUsersKey);
            return members.Select(v => (string)v).ToArray();
        }
    }
}


