using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MatchingService.Domain.Services;
using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly MatchingDomainService _matchingDomainService;

        public TagsController(MatchingDomainService matchingDomainService)
        {
            _matchingDomainService = matchingDomainService;
        }

        /// <summary>
        /// 获取所有标签
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTags([FromQuery] TagCategory? category = null)
        {
            try
            {
                IEnumerable<Tag> tags;
                if (category.HasValue)
                {
                    tags = await _matchingDomainService.GetTagsByCategoryAsync(category.Value);
                }
                else
                {
                    tags = await _matchingDomainService.GetPopularTagsAsync(100);
                }

                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取热门标签
        /// </summary>
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularTags([FromQuery] int count = 20)
        {
            try
            {
                var tags = await _matchingDomainService.GetPopularTagsAsync(count);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取热门标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索标签
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchTags([FromQuery] string keyword, [FromQuery] int count = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest("搜索关键词不能为空");
                }

                var tags = await _matchingDomainService.SearchTagsAsync(keyword, count);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"搜索标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建新标签
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("标签名称不能为空");
                }

                var tag = await _matchingDomainService.CreateTagAsync(request.Name, request.Description ?? "", request.Category);
                return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"创建标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取单个标签
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTag(Guid id)
        {
            try
            {
                var tag = await _matchingDomainService.GetTagByIdAsync(id);
                if (tag == null)
                {
                    return NotFound("标签不存在");
                }

                return Ok(tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"获取标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("标签名称不能为空");
                }

                var tag = await _matchingDomainService.UpdateTagAsync(id, request.Name, request.Description ?? "", request.Category);
                if (tag == null)
                {
                    return NotFound("标签不存在");
                }

                return Ok(tag);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"更新标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            try
            {
                var success = await _matchingDomainService.DeleteTagAsync(id);
                if (!success)
                {
                    return NotFound("标签不存在");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"删除标签失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 创建标签请求
    /// </summary>
    public class CreateTagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TagCategory Category { get; set; }
    }

    /// <summary>
    /// 更新标签请求
    /// </summary>
    public class UpdateTagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TagCategory Category { get; set; }
    }
}
