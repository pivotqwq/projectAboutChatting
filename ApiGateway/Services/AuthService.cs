using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace ApiGateway.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AuthService(ILogger<AuthService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("UserManager");
        _configuration = configuration;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // 本地JWT验证
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // 检查token是否过期
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                return false;
            }

            // 可选：调用用户服务验证token有效性
            // return await ValidateTokenWithUserService(token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return false;
        }
    }

    public async Task<string?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
            return userIdClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract user ID from token");
            return null;
        }
    }

    public async Task<Dictionary<string, string>?> GetUserClaimsAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            var claims = new Dictionary<string, string>();
            foreach (var claim in jsonToken.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
            
            return claims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract claims from token");
            return null;
        }
    }

    private async Task<bool> ValidateTokenWithUserService(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync("/api/users/validate-token");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token with user service");
            return false;
        }
    }
}
