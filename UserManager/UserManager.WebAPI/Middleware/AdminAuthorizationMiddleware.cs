using System.Security.Claims;

namespace UserManager.WebAPI.Middleware
{
    /// <summary>
    /// 管理员权限验证中间件
    /// </summary>
    public class AdminAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminAuthorizationMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public AdminAuthorizationMiddleware(RequestDelegate next, ILogger<AdminAuthorizationMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 检查是否为管理员接口
            if (IsAdminEndpoint(context.Request.Path))
            {
                // 从请求头或查询参数中获取管理员密码
                var adminPassword = context.Request.Headers["X-Admin-Password"].FirstOrDefault() 
                    ?? context.Request.Query["adminPassword"].FirstOrDefault();

                if (string.IsNullOrEmpty(adminPassword))
                {
                    _logger.LogWarning("访问管理员接口缺少管理员密码: {Path}", context.Request.Path);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("缺少管理员密码");
                    return;
                }

                // 验证管理员密码
                if (!await ValidateAdminPassword(adminPassword))
                {
                    _logger.LogWarning("管理员密码验证失败: {Path}", context.Request.Path);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("管理员密码错误");
                    return;
                }

                _logger.LogInformation("管理员接口访问成功: {Path}", context.Request.Path);
            }

            await _next(context);
        }

        private async Task<bool> ValidateAdminPassword(string password)
        {
            try
            {
                // 从配置中获取密码文件路径
                var passwordFilePath = _configuration["AdminPassword:FilePath"] ?? "admin_password.txt";
                
                // 如果路径不是绝对路径，则相对于应用程序根目录
                if (!Path.IsPathRooted(passwordFilePath))
                {
                    var appRoot = Directory.GetCurrentDirectory();
                    passwordFilePath = Path.Combine(appRoot, passwordFilePath);
                }

                if (!File.Exists(passwordFilePath))
                {
                    _logger.LogError("管理员密码文件不存在: {FilePath}", passwordFilePath);
                    return false;
                }

                var storedPassword = await File.ReadAllTextAsync(passwordFilePath);
                return storedPassword.Trim().Equals(password, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证管理员密码时发生错误");
                return false;
            }
        }

        private static bool IsAdminEndpoint(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant();
            return pathValue != null && (
                pathValue.Contains("/usersmgr/adminchangepassword") ||
                pathValue.Contains("/usersmgr/unlock") ||
                pathValue.Contains("/usersmgr/lockuser")
            );
        }
    }
}
