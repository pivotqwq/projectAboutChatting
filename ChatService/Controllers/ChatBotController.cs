using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StackExchange.Redis;

namespace ChatService.Controllers
{
    /// <summary>
    /// 聊天机器人管理接口
    /// </summary>
    [ApiController]
    [Route("api/chatbot")]
    [Authorize]
    [Produces("application/json")]
    public class ChatBotController : ControllerBase
    {
        private readonly AiChatBotService _chatBotService;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ChatBotController> _logger;
        private const int DailyTokenLimit = 3000;

        public ChatBotController(
            AiChatBotService chatBotService, 
            IConnectionMultiplexer redis,
            ILogger<ChatBotController> logger)
        {
            _chatBotService = chatBotService;
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// 从JWT Claims中获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return User?.FindFirstValue("sub")
                   ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User?.Identity?.Name
                   ?? string.Empty;
        }

        /// <summary>
        /// 获取聊天机器人信息
        /// </summary>
        /// <returns>机器人用户ID和状态</returns>
        /// <response code="200">成功返回机器人信息</response>
        [HttpGet("info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetChatBotInfo()
        {
            try
            {
                var botUserId = _chatBotService.GetBotUserId();
                var isConfigured = !string.IsNullOrWhiteSpace(_chatBotService.GetBotUserId());

                return Ok(new
                {
                    botUserId = botUserId,
                    name = "AI助手",
                    description = "智能聊天机器人，基于阿里云百炼千问模型",
                    enabled = isConfigured,
                    supportsVoice = false,  // 机器人只支持文本消息
                    supportsImage = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取聊天机器人信息失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        /// <summary>
        /// 获取当前用户的AI对话token使用情况
        /// </summary>
        /// <returns>今日token使用量和剩余额度</returns>
        /// <response code="200">成功返回使用情况</response>
        [HttpGet("usage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTokenUsage()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { error = "无效的认证信息" });
                }

                var usage = await GetDailyTokenUsageAsync(userId);
                var remaining = Math.Max(0, DailyTokenLimit - usage);

                return Ok(new
                {
                    used = usage,
                    limit = DailyTokenLimit,
                    remaining = remaining,
                    resetTime = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取token使用情况失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        /// <summary>
        /// 获取用户今日token使用量
        /// </summary>
        private async Task<int> GetDailyTokenUsageAsync(string userId)
        {
            try
            {
                var db = _redis.GetDatabase();
                var todayKey = $"chatbot:daily-tokens:{userId}:{DateTime.UtcNow:yyyyMMdd}";
                var usage = await db.StringGetAsync(todayKey);
                return usage.HasValue && int.TryParse(usage, out var value) ? value : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户token使用量失败: UserId={UserId}", userId);
                return 0;
            }
        }
    }
}

