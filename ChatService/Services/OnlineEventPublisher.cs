using StackExchange.Redis;
using System.Text.Json;

namespace ChatService.Services
{
    public class OnlineEventPublisher
    {
        private readonly IConnectionMultiplexer _redis;
        private const string ChannelName = "user-online-events";

        public OnlineEventPublisher(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task PublishUserOnlineAsync(string userId)
        {
            var payload = JsonSerializer.Serialize(new { Type = "Online", UserId = userId, OccurredAt = DateTimeOffset.UtcNow });
            var sub = _redis.GetSubscriber();
            await sub.PublishAsync(ChannelName, payload);
        }

        public async Task PublishUserOfflineAsync(string userId)
        {
            var payload = JsonSerializer.Serialize(new { Type = "Offline", UserId = userId, OccurredAt = DateTimeOffset.UtcNow });
            var sub = _redis.GetSubscriber();
            await sub.PublishAsync(ChannelName, payload);
        }
    }
}


