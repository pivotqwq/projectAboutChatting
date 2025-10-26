using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MatchingService.Domain.Services;
using MatchingService.Domain.Entities;
using System.Security.Claims;

namespace MatchingService.WebAPI.Controllers
{
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    [Authorize]
    public class UserTagsController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public UserTagsController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        /// <summary>
        /// 获取用户的标签列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserTags(Guid userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能查看自己的标签");
                }

                var userTags = await _matchingDomainService.GetUserTagsAsync(userId);
                return Ok(userTags);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取用户标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 为用户添加标签
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddUserTag(Guid userId, [FromBody] AddUserTagRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能为自己添加标签");
                }

                if (request.Weight < 0 || request.Weight > 1)
                {
                    return BadRequest("标签权重必须在0-1之间");
                }

                var userTag = await _matchingDomainService.AddUserTagAsync(userId, request.TagId, request.Weight);
                return CreatedAtAction(nameof(GetUserTag), new { userId, tagId = request.TagId }, userTag);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"添加用户标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取用户的单个标签
        /// </summary>
        [HttpGet("{tagId}")]
        public async Task<IActionResult> GetUserTag(Guid userId, Guid tagId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能查看自己的标签");
                }

                var userTag = await _matchingDomainService.GetUserTagAsync(userId, tagId);
                if (userTag == null)
                {
                    return NotFound("用户标签不存在");
                }

                return Ok(userTag);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取用户标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户标签权重
        /// </summary>
        [HttpPut("{tagId}")]
        public async Task<IActionResult> UpdateUserTag(Guid userId, Guid tagId, [FromBody] UpdateUserTagRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能更新自己的标签");
                }

                if (request.Weight < 0 || request.Weight > 1)
                {
                    return BadRequest("标签权重必须在0-1之间");
                }

                var userTag = await _matchingDomainService.UpdateUserTagAsync(userId, tagId, request.Weight);
                if (userTag == null)
                {
                    return NotFound("用户标签不存在");
                }

                return Ok(userTag);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"更新用户标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除用户标签
        /// </summary>
        [HttpDelete("{tagId}")]
        public async Task<IActionResult> RemoveUserTag(Guid userId, Guid tagId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid("只能移除自己的标签");
                }

                await _matchingDomainService.RemoveUserTagAsync(userId, tagId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("无效的用户身份");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"移除用户标签失败: {ex.Message}");
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
    /// 添加用户标签请求
    /// </summary>
    public class AddUserTagRequest
    {
        public Guid TagId { get; set; }
        public float Weight { get; set; } = 1.0f;
    }

    /// <summary>
    /// 更新用户标签请求
    /// </summary>
    public class UpdateUserTagRequest
    {
        public float Weight { get; set; }
    }
}
