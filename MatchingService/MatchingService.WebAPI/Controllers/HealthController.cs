using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchingService.Infrastructure;
using System.Text.Json;

namespace MatchingService.WebAPI.Controllers
{
    /// <summary>
    /// 健康检查控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly MatchingDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(MatchingDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 基础健康检查
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        /// <summary>
        /// 详细健康检查
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var healthStatus = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                checks = new
                {
                    database = await CheckDatabaseAsync(),
                    memory = CheckMemoryUsage(),
                    uptime = GetUptime()
                }
            };

            var isHealthy = healthStatus.checks.database.Status == "Healthy";
            return isHealthy ? Ok(healthStatus) : StatusCode(503, healthStatus);
        }

        private async Task<HealthCheckResult> CheckDatabaseAsync()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return new HealthCheckResult("Healthy", "数据库连接正常");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库健康检查失败");
                return new HealthCheckResult("Unhealthy", ex.Message);
            }
        }

        private object CheckMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB

            return new
            {
                status = memoryUsage < 1000 ? "Healthy" : "Warning", // 超过1GB警告
                message = $"内存使用: {memoryUsage}MB"
            };
        }

        private object GetUptime()
        {
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return new
            {
                status = "Healthy",
                message = $"运行时间: {uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟"
            };
        }

        private sealed record HealthCheckResult(string Status, string Message);
    }
}
