using System.Collections.Concurrent;

namespace ApiGateway.Services;

public class RateLimitService : IRateLimitService
{
    private readonly ILogger<RateLimitService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits;
    private readonly int _requestsPerMinute;
    private readonly int _burstLimit;

    public RateLimitService(ILogger<RateLimitService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _clientLimits = new ConcurrentDictionary<string, ClientRateLimit>();
        _requestsPerMinute = configuration.GetValue<int>("RateLimit:RequestsPerMinute", 100);
        _burstLimit = configuration.GetValue<int>("RateLimit:BurstLimit", 10);
    }

    public async Task<bool> IsAllowedAsync(string clientId, string endpoint)
    {
        var key = $"{clientId}:{endpoint}";
        
        if (!_clientLimits.TryGetValue(key, out var rateLimit))
        {
            rateLimit = new ClientRateLimit
            {
                LastReset = DateTime.UtcNow,
                RequestCount = 0,
                BurstCount = 0,
                LastBurstReset = DateTime.UtcNow
            };
            _clientLimits.TryAdd(key, rateLimit);
        }

        var now = DateTime.UtcNow;

        // 重置每分钟计数器
        if (now.Subtract(rateLimit.LastReset).TotalMinutes >= 1)
        {
            rateLimit.LastReset = now;
            rateLimit.RequestCount = 0;
        }

        // 重置突发计数器（每10秒重置一次）
        if (now.Subtract(rateLimit.LastBurstReset).TotalSeconds >= 10)
        {
            rateLimit.LastBurstReset = now;
            rateLimit.BurstCount = 0;
        }

        // 检查每分钟限制
        if (rateLimit.RequestCount >= _requestsPerMinute)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
            return false;
        }

        // 检查突发限制
        if (rateLimit.BurstCount >= _burstLimit)
        {
            _logger.LogWarning("Burst limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
            return false;
        }

        // 增加计数器
        Interlocked.Increment(ref rateLimit.RequestCount);
        Interlocked.Increment(ref rateLimit.BurstCount);

        return true;
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string clientId)
    {
        var key = $"{clientId}:global";
        
        if (!_clientLimits.TryGetValue(key, out var rateLimit))
        {
            return new RateLimitInfo
            {
                RemainingRequests = _requestsPerMinute,
                ResetTime = DateTime.UtcNow.AddMinutes(1),
                BurstLimit = _burstLimit,
                BurstRemaining = _burstLimit
            };
        }

        var now = DateTime.UtcNow;
        var remainingRequests = Math.Max(0, _requestsPerMinute - rateLimit.RequestCount);
        var resetTime = rateLimit.LastReset.AddMinutes(1);
        
        var remainingBurst = Math.Max(0, _burstLimit - rateLimit.BurstCount);

        return new RateLimitInfo
        {
            RemainingRequests = remainingRequests,
            ResetTime = resetTime,
            BurstLimit = _burstLimit,
            BurstRemaining = remainingBurst
        };
    }

    private class ClientRateLimit
    {
        public DateTime LastReset { get; set; }
        public int RequestCount { get; set; }
        public int BurstCount { get; set; }
        public DateTime LastBurstReset { get; set; }
    }
}
