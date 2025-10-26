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
    public class RecommendationsController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public RecommendationsController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        /// <summary>
        /// 获取用户推荐列表
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetRecommendations(
            Guid userId,
            [FromQuery] int count = 20,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null)
        {
            try
            {
                // 验证用户权限（只能获取自己的推荐）
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能获取自己的推荐列表");
                }

                Location? userLocation = null;
                if (latitude.HasValue && longitude.HasValue)
                {
                    userLocation = new Location(latitude.Value, longitude.Value);
                }

                var recommendations = await _matchingDomainService.GetUserRecommendationsAsync(userId, userLocation, count);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取推荐失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取基于标签的推荐
        /// </summary>
        [HttpGet("{userId}/tag-based")]
        public async Task<IActionResult> GetTagBasedRecommendations(Guid userId, [FromQuery] int count = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能获取自己的推荐列表");
                }

                var recommendations = await _matchingDomainService.GetTagBasedRecommendationsAsync(userId, count);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取标签推荐失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取协同过滤推荐
        /// </summary>
        [HttpGet("{userId}/collaborative")]
        public async Task<IActionResult> GetCollaborativeFilteringRecommendations(Guid userId, [FromQuery] int count = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能获取自己的推荐列表");
                }

                var recommendations = await _matchingDomainService.GetCollaborativeFilteringRecommendationsAsync(userId, count);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取协同过滤推荐失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取基于地理位置的推荐
        /// </summary>
        [HttpGet("{userId}/location")]
        public async Task<IActionResult> GetLocationBasedRecommendations(
            Guid userId,
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double maxDistanceKm = 50,
            [FromQuery] int count = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能获取自己的推荐列表");
                }

                var userLocation = new Location(latitude, longitude);
                var recommendations = await _matchingDomainService.GetLocationBasedRecommendationsAsync(userId, userLocation, maxDistanceKm, count);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取地理位置推荐失败: {ex.Message}");
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
}
