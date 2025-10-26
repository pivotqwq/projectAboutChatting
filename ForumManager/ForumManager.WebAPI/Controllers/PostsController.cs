using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ForumManager.Domain;
using ForumManager.Domain.ValueObjects;
using ForumManager.WebAPI.Models.Requests;
using ForumManager.WebAPI.Models.Responses;

namespace ForumManager.WebAPI.Controllers
{
    /// <summary>
    /// 帖子管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ForumDomainService _forumDomainService;
        private readonly IForumRepository _forumRepository;

        public PostsController(ForumDomainService forumDomainService, IForumRepository forumRepository)
        {
            _forumDomainService = forumDomainService;
            _forumRepository = forumRepository;
        }

        /// <summary>
        /// 获取帖子列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<PostResponse>>> GetPosts(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 20,
            [FromQuery] PostCategory? category = null,
            [FromQuery] string? keyword = null)
        {
            if (pageSize > 100) pageSize = 100;

            var posts = await _forumRepository.GetPostsAsync(pageIndex, pageSize, category, keyword);
            var totalCount = await _forumRepository.GetPostCountAsync(category);

            var postResponses = posts.Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                Category = p.Category,
                AuthorId = p.AuthorId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ViewCount = p.ViewCount,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                FavoriteCount = p.FavoriteCount,
                Tags = p.Tags
            }).ToList();

            return Ok(new PagedResponse<PostResponse>
            {
                Data = postResponses,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        /// <summary>
        /// 获取热门帖子
        /// </summary>
        [HttpGet("hot")]
        public async Task<ActionResult<List<PostResponse>>> GetHotPosts([FromQuery] int count = 10)
        {
            if (count > 50) count = 50;

            var posts = await _forumRepository.GetHotPostsAsync(count);
            var postResponses = posts.Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                Category = p.Category,
                AuthorId = p.AuthorId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ViewCount = p.ViewCount,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                FavoriteCount = p.FavoriteCount,
                Tags = p.Tags
            }).ToList();

            return Ok(postResponses);
        }

        /// <summary>
        /// 获取帖子详情
        /// </summary>
        [HttpGet("{postId}")]
        public async Task<ActionResult<PostDetailResponse>> GetPost(Guid postId)
        {
            var post = await _forumRepository.GetPostByIdAsync(postId);
            if (post == null)
                return NotFound("帖子不存在");

            // 增加浏览次数
            post.IncrementViewCount();
            await _forumRepository.UpdatePostAsync(post);

            var response = new PostDetailResponse
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Category = post.Category,
                AuthorId = post.AuthorId,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ViewCount = post.ViewCount,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                FavoriteCount = post.FavoriteCount,
                Tags = post.Tags,
                Comments = post.Comments.Select(c => new CommentResponse
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorId = c.AuthorId,
                    ParentCommentId = c.ParentCommentId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    LikeCount = c.LikeCount
                }).ToList()
            };

            return Ok(response);
        }

        /// <summary>
        /// 创建帖子
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PostResponse>> CreatePost([FromBody] CreatePostRequest request)
        {
            var userId = GetCurrentUserId();
            var post = await _forumDomainService.CreatePostAsync(userId, request.Title, request.Content, request.Category, request.Tags);

            var response = new PostResponse
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Category = post.Category,
                AuthorId = post.AuthorId,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ViewCount = post.ViewCount,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                FavoriteCount = post.FavoriteCount,
                Tags = post.Tags
            };

            return CreatedAtAction(nameof(GetPost), new { postId = post.Id }, response);
        }

        /// <summary>
        /// 编辑帖子
        /// </summary>
        [HttpPut("{postId}")]
        [Authorize]
        public async Task<ActionResult<PostResponse>> UpdatePost(Guid postId, [FromBody] UpdatePostRequest request)
        {
            var userId = GetCurrentUserId();
            var post = await _forumDomainService.EditPostAsync(postId, userId, request.Title, request.Content, request.Tags);

            var response = new PostResponse
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                Category = post.Category,
                AuthorId = post.AuthorId,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ViewCount = post.ViewCount,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                FavoriteCount = post.FavoriteCount,
                Tags = post.Tags
            };

            return Ok(response);
        }

        /// <summary>
        /// 删除帖子
        /// </summary>
        [HttpDelete("{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(Guid postId)
        {
            var userId = GetCurrentUserId();
            await _forumDomainService.DeletePostAsync(postId, userId);
            return NoContent();
        }

        /// <summary>
        /// 点赞/取消点赞帖子
        /// </summary>
        [HttpPost("{postId}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(Guid postId)
        {
            var userId = GetCurrentUserId();
            await _forumDomainService.TogglePostLikeAsync(postId, userId);
            return Ok();
        }

        /// <summary>
        /// 收藏/取消收藏帖子
        /// </summary>
        [HttpPost("{postId}/favorite")]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(Guid postId)
        {
            var userId = GetCurrentUserId();
            await _forumDomainService.TogglePostFavoriteAsync(postId, userId);
            return Ok();
        }

        /// <summary>
        /// 获取用户帖子列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PagedResponse<PostResponse>>> GetUserPosts(
            Guid userId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 20)
        {
            if (pageSize > 100) pageSize = 100;

            var posts = await _forumRepository.GetUserPostsAsync(userId, pageIndex, pageSize);
            var totalCount = await _forumRepository.GetPostCountAsync(); // 这里应该实现按用户统计

            var postResponses = posts.Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                Category = p.Category,
                AuthorId = p.AuthorId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ViewCount = p.ViewCount,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                FavoriteCount = p.FavoriteCount,
                Tags = p.Tags
            }).ToList();

            return Ok(new PagedResponse<PostResponse>
            {
                Data = postResponses,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
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
