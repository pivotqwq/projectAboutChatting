using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;
using System.Security.Claims;

namespace MatchingService.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InteractionsController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public InteractionsController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        /// <summary>
        /// 记录用户交互
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RecordInteraction([FromBody] RecordInteractionRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // 验证交互类型
                if (!Enum.IsDefined(typeof(InteractionType), request.Type))
                {
                    return BadRequest("无效的交互类型");
                }

                // 验证评分范围
                if (request.Rating < 0 || request.Rating > 5)
                {
                    return BadRequest("评分必须在0-5之间");
                }

                await _matchingDomainService.RecordUserInteractionAsync(
                    currentUserId, 
                    request.TargetUserId, 
                    request.Type, 
                    request.Rating, 
                    request.Context);

                return Ok(new { message = "交互记录成功" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"记录交互失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取用户交互历史
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserInteractions(
            Guid userId,
            [FromQuery] InteractionType? type = null,
            [FromQuery] Guid? targetUserId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能查看自己的交互记录");
                }

                // 验证分页参数
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // 这里需要实现分页查询逻辑
                // 暂时返回空结果，等待仓储实现
                var result = new
                {
                    items = new List<object>(),
                    totalCount = 0,
                    page = page,
                    pageSize = pageSize,
                    totalPages = 0
                };

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取交互记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取用户交互统计
        /// </summary>
        [HttpGet("users/{userId}/stats")]
        public async Task<IActionResult> GetUserInteractionStats(Guid userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能查看自己的统计信息");
                }

                // 这里需要实现统计逻辑
                // 暂时返回模拟数据
                var stats = new
                {
                    totalInteractions = 0,
                    interactionsByType = new Dictionary<string, int>(),
                    averageRating = 0.0f,
                    recentActivity = new List<object>()
                };

                return Ok(stats);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取统计信息失败: {ex.Message}");
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new UnauthorizedAccessException("无效的用户ID");
            }
            return userId;
        }
    }

    /// <summary>
    /// 记录交互请求
    /// </summary>
    public class RecordInteractionRequest
    {
        public Guid TargetUserId { get; set; }
        public InteractionType Type { get; set; }
        public float Rating { get; set; } = 0.0f;
        public string? Context { get; set; }
    }
}
