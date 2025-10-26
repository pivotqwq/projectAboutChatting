using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Controllers
{
    /// <summary>
    /// 在线状态管理接口 - 提供用户在线状态查询功能
    /// </summary>
    [ApiController]
    [Route("api/presence")]
    [Authorize]
    [Produces("application/json")]
    public class PresenceController : ControllerBase
    {
        private readonly PresenceService _presence;
        private readonly ILogger<PresenceController> _logger;

        public PresenceController(PresenceService presence, ILogger<PresenceController> logger)
        {
            _presence = presence;
            _logger = logger;
        }

        /// <summary>
        /// 从JWT Claims中获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            // UserManager使用 "sub" claim 存储用户ID
            return User?.FindFirstValue("sub")  // JWT标准的Subject claim
                   ?? User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                   ?? User?.Identity?.Name 
                   ?? string.Empty;
        }

        /// <summary>
        /// 获取所有在线用户列表
        /// </summary>
        /// <returns>在线用户ID列表</returns>
        /// <response code="200">成功返回在线用户列表</response>
        /// <remarks>
        /// 此接口允许匿名访问，不需要认证。
        /// 用户上线状态由 SignalR Hub 连接自动管理。
        /// </remarks>
        [HttpGet("online-users")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOnlineUsers()
        {
            try
            {
                _logger.LogInformation("请求获取所有在线用户");
                var userIds = await _presence.GetOnlineUsersAsync();
                _logger.LogInformation("当前在线用户数量: {Count}", userIds.Length);
                return Ok(userIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取在线用户列表失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        /// <summary>
        /// 检查指定用户是否在线
        /// </summary>
        /// <param name="userId">要检查的用户ID</param>
        /// <returns>用户在线状态</returns>
        /// <response code="200">成功返回用户在线状态</response>
        /// <response code="400">参数错误</response>
        /// <response code="401">未授权访问</response>
        [HttpGet("is-online/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> IsOnline([FromRoute] string userId)
        {
            try
            {
                // 验证参数
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { error = "userId 不能为空" });
                }

                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("用户 {CurrentUserId} 查询用户 {TargetUserId} 的在线状态", currentUserId, userId);

                var online = await _presence.IsOnlineAsync(userId);
                
                return Ok(new { userId, online });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户在线状态失败，用户ID: {UserId}", userId);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }
    }
}


