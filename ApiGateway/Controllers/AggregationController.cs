using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/aggregated")]
[Authorize]
public class AggregationController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly ILogger<AggregationController> _logger;

    public AggregationController(IAggregationService aggregationService, ILogger<AggregationController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户资料及其帖子列表
    /// </summary>
    [HttpGet("user/{userId}/profile-with-posts")]
    public async Task<IActionResult> GetUserProfileWithPosts(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // 只有用户本人或管理员可以查看详细资料
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid("You can only view your own profile");
            }

            var result = await _aggregationService.GetUserProfileWithPostsAsync(userId, page, pageSize);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile with posts for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 获取帖子及其评论列表
    /// </summary>
    [HttpGet("post/{postId}/with-comments")]
    public async Task<IActionResult> GetPostWithComments(
        string postId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _aggregationService.GetPostWithCommentsAsync(postId, page, pageSize);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get post with comments for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 获取用户仪表板数据
    /// </summary>
    [HttpGet("user/dashboard")]
    public async Task<IActionResult> GetUserDashboard()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _aggregationService.GetUserDashboardAsync(userId);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user dashboard");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 获取论坛统计信息
    /// </summary>
    [HttpGet("forum/stats")]
    public async Task<IActionResult> GetForumStats()
    {
        try
        {
            var result = await _aggregationService.GetForumStatsAsync();
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get forum stats");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// 健康检查端点
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "ApiGateway"
        });
    }
}
