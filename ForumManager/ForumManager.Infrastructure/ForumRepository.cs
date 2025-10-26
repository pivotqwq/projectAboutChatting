using ForumManager.Domain;
using ForumManager.Domain.Entities;
using ForumManager.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ForumManager.Infrastructure
{
    /// <summary>
    /// 论坛仓储实现
    /// </summary>
    public class ForumRepository : IForumRepository
    {
        private readonly ForumDBContext _context;
        private readonly IDistributedCache _cache;

        public ForumRepository(ForumDBContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        #region 帖子相关

        public async Task<Post?> GetPostByIdAsync(Guid postId)
        {
            // 不使用缓存，直接从数据库获取以确保 EF Core 正确跟踪
            // 缓存反序列化的实体会导致跟踪问题
            var post = await _context.Posts
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.Likes)
                .Include(p => p.Favorites)
                .FirstOrDefaultAsync(p => p.Id == postId);

            return post;
        }

        public async Task<List<Post>> GetPostsAsync(int pageIndex, int pageSize, PostCategory? category = null, string? keyword = null)
        {
            var query = _context.Posts
                .Where(p => p.Status == PostStatus.Published)
                .AsQueryable();

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Title.Contains(keyword) || p.Content.Contains(keyword));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetHotPostsAsync(int count)
        {
            // 不缓存完整实体以避免 EF Core 跟踪问题
            // 如需缓存，应该缓存 DTO 而不是实体
            var posts = await _context.Posts
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.ViewCount * 0.3 + p.LikeCount * 0.5 + p.CommentCount * 0.2)
                .Take(count)
                .ToListAsync();

            return posts;
        }

        public async Task<List<Post>> GetUserPostsAsync(Guid userId, int pageIndex, int pageSize)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == userId && p.Status != PostStatus.Deleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Post> CreatePostAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> UpdatePostAsync(Post post)
        {
            // 如果实体已被跟踪，不需要调用 Update
            var entry = _context.Entry(post);
            if (entry.State == EntityState.Detached)
            {
                _context.Posts.Update(post);
            }
            
            await _context.SaveChangesAsync();

            return post;
        }

        public async Task DeletePostAsync(Guid postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.Status = PostStatus.Deleted;
                await _context.SaveChangesAsync();

                // 清除缓存
                var cacheKey = $"post:{postId}";
                await _cache.RemoveAsync(cacheKey);
            }
        }

        #endregion

        #region 评论相关

        public async Task<Comment?> GetCommentByIdAsync(Guid commentId)
        {
            return await _context.Comments
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == commentId);
        }

        public async Task<List<Comment>> GetPostCommentsAsync(Guid postId, int pageIndex, int pageSize)
        {
            return await _context.Comments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Comment> CreateCommentAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment> UpdateCommentAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteCommentAsync(Guid commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment != null)
            {
                comment.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region 点赞相关

        public async Task<bool> IsPostLikedByUserAsync(Guid postId, Guid userId)
        {
            return await _context.PostLikes
                .AnyAsync(pl => pl.PostId == postId && pl.UserId == userId);
        }

        public async Task<bool> IsCommentLikedByUserAsync(Guid commentId, Guid userId)
        {
            return await _context.CommentLikes
                .AnyAsync(cl => cl.CommentId == commentId && cl.UserId == userId);
        }

        public async Task<bool> IsPostFavoritedByUserAsync(Guid postId, Guid userId)
        {
            return await _context.PostFavorites
                .AnyAsync(pf => pf.PostId == postId && pf.UserId == userId);
        }

        public async Task TogglePostLikeAsync(Guid postId, Guid userId)
        {
            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

            if (existingLike != null)
            {
                // 取消点赞
                _context.PostLikes.Remove(existingLike);
                
                // 更新帖子点赞数
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    post.LikeCount = Math.Max(0, post.LikeCount - 1);
                }
            }
            else
            {
                // 添加点赞
                var newLike = new PostLike(postId, userId);
                _context.PostLikes.Add(newLike);
                
                // 更新帖子点赞数
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    post.LikeCount++;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task TogglePostFavoriteAsync(Guid postId, Guid userId)
        {
            var existingFavorite = await _context.PostFavorites
                .FirstOrDefaultAsync(pf => pf.PostId == postId && pf.UserId == userId);

            if (existingFavorite != null)
            {
                // 取消收藏
                _context.PostFavorites.Remove(existingFavorite);
                
                // 更新帖子收藏数
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    post.FavoriteCount = Math.Max(0, post.FavoriteCount - 1);
                }
            }
            else
            {
                // 添加收藏
                var newFavorite = new PostFavorite(postId, userId);
                _context.PostFavorites.Add(newFavorite);
                
                // 更新帖子收藏数
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    post.FavoriteCount++;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task ToggleCommentLikeAsync(Guid commentId, Guid userId)
        {
            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);

            if (existingLike != null)
            {
                // 取消点赞
                _context.CommentLikes.Remove(existingLike);
                
                // 更新评论点赞数
                var comment = await _context.Comments.FindAsync(commentId);
                if (comment != null)
                {
                    comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                }
            }
            else
            {
                // 添加点赞
                var newLike = new CommentLike(commentId, userId);
                _context.CommentLikes.Add(newLike);
                
                // 更新评论点赞数
                var comment = await _context.Comments.FindAsync(commentId);
                if (comment != null)
                {
                    comment.LikeCount++;
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region 统计相关

        public async Task<int> GetPostCountAsync(PostCategory? category = null)
        {
            var query = _context.Posts
                .Where(p => p.Status == PostStatus.Published)
                .AsQueryable();

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            return await query.CountAsync();
        }

        public async Task<int> GetCommentCountAsync(Guid postId)
        {
            return await _context.Comments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetLikeCountAsync(Guid postId)
        {
            return await _context.PostLikes
                .Where(pl => pl.PostId == postId)
                .CountAsync();
        }

        public async Task<int> GetFavoriteCountAsync(Guid postId)
        {
            return await _context.PostFavorites
                .Where(pf => pf.PostId == postId)
                .CountAsync();
        }


        #endregion
    }
}
