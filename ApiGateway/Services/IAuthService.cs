namespace ApiGateway.Services;

public interface IAuthService
{
    Task<bool> ValidateTokenAsync(string token);
    Task<string?> GetUserIdFromTokenAsync(string token);
    Task<Dictionary<string, string>?> GetUserClaimsAsync(string token);
}
