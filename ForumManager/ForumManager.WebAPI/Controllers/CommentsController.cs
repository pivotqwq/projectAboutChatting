using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ForumManager.Domain;
using ForumManager.WebAPI.Models.Requests;
using ForumManager.WebAPI.Models.Responses;
using Microsoft.Extensions.Logging;

namespace ForumManager.WebAPI.Controllers
{
    /// <summary>
    /// 评论管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ForumDomainService _forumDomainService;
        private readonly IForumRepository _forumRepository;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            ForumDomainService forumDomainService,
            IForumRepository forumRepository,
            ILogger<CommentsController> logger)
        {
            _forumDomainService = forumDomainService;
            _forumRepository = forumRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取帖子的评论列表
        /// </summary>
        [HttpGet("post/{postId}")]
        public async Task<ActionResult<List<CommentResponse>>> GetPostComments(
            Guid postId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 20)
        {
            if (pageSize > 100) pageSize = 100;

            var comments = await _forumRepository.GetPostCommentsAsync(postId, pageIndex, pageSize);
            var commentResponses = comments.Select(c => new CommentResponse
            {
                Id = c.Id,
                Content = c.Content,
                AuthorId = c.AuthorId,
                ParentCommentId = c.ParentCommentId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                LikeCount = c.LikeCount
            }).ToList();

            return Ok(commentResponses);
        }

        /// <summary>
        /// 获取评论详情
        /// </summary>
        [HttpGet("{commentId}")]
        public async Task<ActionResult<CommentResponse>> GetComment(Guid commentId)
        {
            var comment = await _forumRepository.GetCommentByIdAsync(commentId);
            if (comment == null)
                return NotFound("评论不存在");

            var response = new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorId = comment.AuthorId,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                LikeCount = comment.LikeCount
            };

            return Ok(response);
        }

        /// <summary>
        /// 添加评论
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CreateCommentRequest request)
        {
            var userId = GetCurrentUserId();
            var comment = await _forumDomainService.AddCommentAsync(request.PostId, userId, request.Content, request.ParentCommentId);

            var response = new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorId = comment.AuthorId,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                LikeCount = comment.LikeCount
            };

            return CreatedAtAction(nameof(GetComment), new { commentId = comment.Id }, response);
        }

        /// <summary>
        /// 编辑评论
        /// </summary>
        [HttpPut("{commentId}")]
        [Authorize]
        public async Task<ActionResult<CommentResponse>> UpdateComment(Guid commentId, [FromBody] UpdateCommentRequest request)
        {
            var userId = GetCurrentUserId();
            var comment = await _forumDomainService.EditCommentAsync(commentId, userId, request.Content);

            var response = new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorId = comment.AuthorId,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                LikeCount = comment.LikeCount
            };

            return Ok(response);
        }

        /// <summary>
        /// 删除评论
        /// </summary>
        [HttpDelete("{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            await _forumDomainService.DeleteCommentAsync(commentId, userId);
            return NoContent();
        }

        /// <summary>
        /// 点赞/取消点赞评论
        /// </summary>
        [HttpPost("{commentId}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(Guid commentId)
        {
            var userId = GetCurrentUserId();
            try
            {
                await _forumDomainService.ToggleCommentLikeAsync(commentId, userId);
            }
            catch (ArgumentException)
            {
                return NotFound("评论不存在");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toggle comment like failed for comment {CommentId}", commentId);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
            return Ok();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("无效的用户ID");
            return userId;
        }
    }
}
