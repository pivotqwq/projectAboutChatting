using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace ApiGateway.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // 跳过认证的路径
        if (ShouldSkipAuthentication(path))
        {
            await _next(context);
            return;
        }

        try
        {
            var token = ExtractTokenFromRequest(context);
            
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: No token provided");
                return;
            }

            // 验证并解析JWT token
            var principal = ValidateToken(token);
            if (principal == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid token");
                return;
            }

            // 将用户信息添加到请求头中，传递给下游服务
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = principal.FindFirst(ClaimTypes.Name)?.Value;
            var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                context.Request.Headers.Add("X-User-Id", userId);
            }
            if (!string.IsNullOrEmpty(userName))
            {
                context.Request.Headers.Add("X-User-Name", userName);
            }
            if (!string.IsNullOrEmpty(userEmail))
            {
                context.Request.Headers.Add("X-User-Email", userEmail);
            }

            // 将用户信息添加到上下文中
            context.Items["UserId"] = userId;
            context.Items["UserName"] = userName;
            context.Items["UserEmail"] = userEmail;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication middleware error");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error during authentication");
        }
    }

    private bool ShouldSkipAuthentication(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/api/users/register",
            "/api/users/login",
            "/api/users/forgot-password",
            "/api/users/reset-password",
            "/api/users/verify-email",
            "/api/users/verify-phone"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private string? ExtractTokenFromRequest(HttpContext context)
    {
        // 从Authorization header中提取token
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // 从查询参数中提取token（用于WebSocket连接）
        var tokenFromQuery = context.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tokenFromQuery))
        {
            return tokenFromQuery;
        }

        return null;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // 检查token是否过期
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token has expired");
                return null;
            }

            // 创建ClaimsPrincipal
            var claims = jsonToken.Claims;
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }
}
