using System.Collections.Concurrent;
using System.Net;

namespace ApiGateway.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = new RateLimitOptions
        {
            RequestsPerMinute = configuration.GetValue<int>("RateLimit:RequestsPerMinute", 100),
            BurstLimit = configuration.GetValue<int>("RateLimit:BurstLimit", 10)
        };
        _clientLimits = new ConcurrentDictionary<string, ClientRateLimit>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        
        if (!_clientLimits.TryGetValue(clientId, out var rateLimit))
        {
            rateLimit = new ClientRateLimit
            {
                LastReset = DateTime.UtcNow,
                RequestCount = 0,
                BurstCount = 0
            };
            _clientLimits.TryAdd(clientId, rateLimit);
        }

        var now = DateTime.UtcNow;

        // 重置计数器（每分钟重置一次）
        if (now.Subtract(rateLimit.LastReset).TotalMinutes >= 1)
        {
            rateLimit.LastReset = now;
            rateLimit.RequestCount = 0;
            rateLimit.BurstCount = 0;
        }

        // 检查每分钟请求限制
        if (rateLimit.RequestCount >= _options.RequestsPerMinute)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId}. Requests: {RequestCount}/{Limit}", 
                clientId, rateLimit.RequestCount, _options.RequestsPerMinute);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        // 检查突发请求限制（短时间内的快速请求）
        if (rateLimit.BurstCount >= _options.BurstLimit)
        {
            _logger.LogWarning("Burst limit exceeded for client {ClientId}. Burst requests: {BurstCount}/{Limit}", 
                clientId, rateLimit.BurstCount, _options.BurstLimit);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", "10");
            await context.Response.WriteAsync("Too many requests in a short time. Please slow down.");
            return;
        }

        // 增加计数器
        Interlocked.Increment(ref rateLimit.RequestCount);
        Interlocked.Increment(ref rateLimit.BurstCount);

        // 添加响应头
        context.Response.Headers.Add("X-RateLimit-Limit", _options.RequestsPerMinute.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", 
            Math.Max(0, _options.RequestsPerMinute - rateLimit.RequestCount).ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", 
            rateLimit.LastReset.AddMinutes(1).ToUnixTimeSeconds().ToString());

        await _next(context);

        // 突发计数器衰减（每次请求后减少突发计数）
        if (rateLimit.BurstCount > 0)
        {
            Interlocked.Decrement(ref rateLimit.BurstCount);
        }
    }

    private string GetClientId(HttpContext context)
    {
        // 优先使用认证用户ID
        var userId = context.Items["UserId"]?.ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // 使用IP地址作为客户端标识
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
        {
            return "unknown";
        }

        return $"ip:{ipAddress}";
    }

    private class RateLimitOptions
    {
        public int RequestsPerMinute { get; set; }
        public int BurstLimit { get; set; }
    }

    private class ClientRateLimit
    {
        public DateTime LastReset { get; set; }
        public int RequestCount { get; set; }
        public int BurstCount { get; set; }
    }
}
