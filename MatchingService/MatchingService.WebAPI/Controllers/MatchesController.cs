using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MatchingService.Domain.Services;
using MatchingService.Domain.ValueObjects;
using System.Security.Claims;
using MatchType = MatchingService.Domain.ValueObjects.MatchType;

namespace MatchingService.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public MatchesController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        /// <summary>
        /// 获取用户匹配列表
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserMatches(
            Guid userId,
            [FromQuery] MatchStatus? status = null,
            [FromQuery] MatchType? matchType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能查看自己的匹配记录");
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
                return StatusCode(500, $"获取匹配记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新匹配状态
        /// </summary>
        [HttpPut("{matchId}/status")]
        public async Task<IActionResult> UpdateMatchStatus(Guid matchId, [FromBody] UpdateMatchStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // 验证匹配状态
                if (!Enum.IsDefined(typeof(MatchStatus), request.Status))
                {
                    return BadRequest("无效的匹配状态");
                }

                // 这里需要实现更新逻辑
                // 暂时返回成功响应
                var result = new
                {
                    matchId = matchId,
                    status = request.Status,
                    notes = request.Notes,
                    updatedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"更新匹配状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取匹配统计信息
        /// </summary>
        [HttpGet("users/{userId}/stats")]
        public async Task<IActionResult> GetMatchStats(Guid userId)
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
                    totalMatches = 0,
                    pendingMatches = 0,
                    acceptedMatches = 0,
                    rejectedMatches = 0,
                    averageMatchScore = 0.0f,
                    matchesByType = new Dictionary<string, int>(),
                    recentMatches = new List<object>()
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

        /// <summary>
        /// 获取高匹配分数的记录
        /// </summary>
        [HttpGet("users/{userId}/high-score")]
        public async Task<IActionResult> GetHighScoreMatches(
            Guid userId,
            [FromQuery] float minScore = 0.7f,
            [FromQuery] int count = 10)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能查看自己的匹配记录");
                }

                // 验证参数
                if (minScore < 0 || minScore > 1)
                {
                    return BadRequest("最小分数必须在0-1之间");
                }

                if (count < 1 || count > 50)
                {
                    count = 10;
                }

                // 这里需要实现高分数匹配查询逻辑
                // 暂时返回空结果
                var result = new List<object>();

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取高分数匹配失败: {ex.Message}");
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
    /// 更新匹配状态请求
    /// </summary>
    public class UpdateMatchStatusRequest
    {
        public MatchStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
