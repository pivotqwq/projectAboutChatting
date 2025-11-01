using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Controllers
{
    /// <summary>
    /// 频道管理接口 - 提供随机频道分配功能
    /// </summary>
    [ApiController]
    [Route("api/channels")]
    [Authorize]
    [Produces("application/json")]
    public class ChannelsController : ControllerBase
    {
        private readonly ILogger<ChannelsController> _logger;
        private readonly IConfiguration _configuration;

        // 预定义的频道列表（可以根据需要扩展）
        private static readonly string[] DefaultChannels = new[]
        {
            "channel-001",
            "channel-002",
            "channel-003",
            "channel-004",
            "channel-005",
            "channel-006",
            "channel-007",
            "channel-008",
            "channel-009",
            "channel-010"
        };

        public ChannelsController(ILogger<ChannelsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 从JWT Claims中获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return User?.FindFirstValue("sub") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        /// <summary>
        /// 获取随机分配的频道ID
        /// </summary>
        /// <returns>随机分配的频道ID</returns>
        /// <response code="200">成功返回频道ID</response>
        /// <response code="401">未授权访问</response>
        [HttpGet("assign-random")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult AssignRandomChannel()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    _logger.LogWarning("未能从JWT中获取用户ID");
                    return Unauthorized(new { error = "无效的认证信息" });
                }

                // 随机选择一个频道（可以根据用户ID做哈希分配，保证同一用户总是分配到同一频道）
                var random = new Random(currentUserId.GetHashCode());
                var channelId = DefaultChannels[random.Next(DefaultChannels.Length)];

                _logger.LogInformation("用户 {CurrentUserId} 被分配到频道 {ChannelId}", currentUserId, channelId);

                return Ok(new { channelId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配随机频道失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        /// <summary>
        /// 获取所有可用频道列表
        /// </summary>
        /// <returns>可用频道列表</returns>
        /// <response code="200">成功返回频道列表</response>
        [HttpGet("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetChannelList()
        {
            try
            {
                return Ok(DefaultChannels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取频道列表失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }
    }
}

