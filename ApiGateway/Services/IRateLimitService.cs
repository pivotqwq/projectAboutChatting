namespace ApiGateway.Services;

public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string clientId, string endpoint);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string clientId);
}

public class RateLimitInfo
{
    public int RemainingRequests { get; set; }
    public DateTime ResetTime { get; set; }
    public int BurstLimit { get; set; }
    public int BurstRemaining { get; set; }
}
