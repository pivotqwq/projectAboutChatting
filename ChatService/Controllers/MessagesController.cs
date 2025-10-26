using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Controllers
{
    /// <summary>
    /// 消息管理接口 - 提供私聊和群聊消息历史查询功能
    /// </summary>
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    [Produces("application/json")]
    public class MessagesController : ControllerBase
    {
        private readonly MessageRepository _repo;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(MessageRepository repo, ILogger<MessagesController> logger)
        {
            _repo = repo;
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
        /// 获取私聊历史消息
        /// </summary>
        /// <param name="userId">对方用户ID</param>
        /// <param name="beforeUtc">获取此时间之前的消息（可选，ISO 8601 UTC格式）</param>
        /// <param name="pageSize">每页消息数量（默认50，最大200）</param>
        /// <returns>消息列表（按时间升序）</returns>
        /// <response code="200">成功返回消息列表</response>
        /// <response code="400">参数错误</response>
        /// <response code="401">未授权访问</response>
        [HttpGet("private/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPrivateHistory(
            [FromRoute] string userId, 
            [FromQuery] DateTime? beforeUtc, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // 验证参数
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { error = "userId 不能为空" });
                }

                // 获取当前用户ID
                var currentUserId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    _logger.LogWarning("未能从JWT中获取用户ID");
                    return Unauthorized(new { error = "无效的认证信息" });
                }

                // 验证页面大小
                if (pageSize <= 0)
                {
                    pageSize = 50;
                }
                else if (pageSize > 200)
                {
                    pageSize = 200;
                }

                _logger.LogInformation(
                    "用户 {CurrentUserId} 请求与用户 {TargetUserId} 的私聊历史，pageSize: {PageSize}, beforeUtc: {BeforeUtc}",
                    currentUserId, userId, pageSize, beforeUtc);

                var messages = await _repo.GetPrivateHistoryAsync(currentUserId, userId, beforeUtc, pageSize);
                
                // 转换BsonDocument为普通对象，避免序列化类型冲突
                var result = messages.Select(doc => MongoDB.Bson.BsonTypeMapper.MapToDotNetValue(doc)).ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取私聊历史失败，用户ID: {UserId}", userId);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        /// <summary>
        /// 获取群聊历史消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="beforeUtc">获取此时间之前的消息（可选，ISO 8601 UTC格式）</param>
        /// <param name="pageSize">每页消息数量（默认50，最大200）</param>
        /// <returns>消息列表（按时间升序）</returns>
        /// <response code="200">成功返回消息列表</response>
        /// <response code="400">参数错误</response>
        /// <response code="401">未授权访问</response>
        [HttpGet("group/{groupId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetGroupHistory(
            [FromRoute] string groupId, 
            [FromQuery] DateTime? beforeUtc, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // 验证参数
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return BadRequest(new { error = "groupId 不能为空" });
                }

                // 获取当前用户ID（用于日志）
                var currentUserId = GetCurrentUserId();

                // 验证页面大小
                if (pageSize <= 0)
                {
                    pageSize = 50;
                }
                else if (pageSize > 200)
                {
                    pageSize = 200;
                }

                _logger.LogInformation(
                    "用户 {CurrentUserId} 请求群组 {GroupId} 的历史消息，pageSize: {PageSize}, beforeUtc: {BeforeUtc}",
                    currentUserId, groupId, pageSize, beforeUtc);

                var messages = await _repo.GetGroupHistoryAsync(groupId, beforeUtc, pageSize);
                
                // 转换BsonDocument为普通对象，避免序列化类型冲突
                var result = messages.Select(doc => MongoDB.Bson.BsonTypeMapper.MapToDotNetValue(doc)).ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取群聊历史失败，群组ID: {GroupId}", groupId);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }
    }
}


