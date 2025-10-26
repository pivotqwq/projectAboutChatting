using ForumManager.Domain.Entities;
using ForumManager.Domain.ValueObjects;

namespace ForumManager.Domain
{
    /// <summary>
    /// 论坛领域服务
    /// </summary>
    public class ForumDomainService
    {
        private readonly IForumRepository _forumRepository;

        public ForumDomainService(IForumRepository forumRepository)
        {
            _forumRepository = forumRepository;
        }

        /// <summary>
        /// 创建帖子
        /// </summary>
        public async Task<Post> CreatePostAsync(Guid authorId, string title, string content, ValueObjects.PostCategory category, List<string>? tags = null)
        {
            // 验证标题长度
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
                throw new ArgumentException("标题长度必须在1-200字符之间");

            // 验证内容长度
            if (string.IsNullOrWhiteSpace(content) || content.Length > 10000)
                throw new ArgumentException("内容长度必须在1-10000字符之间");

            // 验证标签数量
            if (tags != null && tags.Count > 10)
                throw new ArgumentException("标签数量不能超过10个");

            var post = new Post(authorId, title.Trim(), content.Trim(), category, tags);
            return await _forumRepository.CreatePostAsync(post);
        }

        /// <summary>
        /// 编辑帖子
        /// </summary>
        public async Task<Post> EditPostAsync(Guid postId, Guid userId, string title, string content, List<string>? tags = null)
        {
            var post = await _forumRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new ArgumentException("帖子不存在");

            if (post.AuthorId != userId)
                throw new UnauthorizedAccessException("只能编辑自己的帖子");

            // 验证标题长度
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
                throw new ArgumentException("标题长度必须在1-200字符之间");

            // 验证内容长度
            if (string.IsNullOrWhiteSpace(content) || content.Length > 10000)
                throw new ArgumentException("内容长度必须在1-10000字符之间");

            // 验证标签数量
            if (tags != null && tags.Count > 10)
                throw new ArgumentException("标签数量不能超过10个");

            post.EditPost(title.Trim(), content.Trim(), tags);
            return await _forumRepository.UpdatePostAsync(post);
        }

        /// <summary>
        /// 删除帖子
        /// </summary>
        public async Task DeletePostAsync(Guid postId, Guid userId)
        {
            var post = await _forumRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new ArgumentException("帖子不存在");

            if (post.AuthorId != userId)
                throw new UnauthorizedAccessException("只能删除自己的帖子");

            post.DeletePost();
            await _forumRepository.UpdatePostAsync(post);
        }

        /// <summary>
        /// 添加评论
        /// </summary>
        public async Task<Comment> AddCommentAsync(Guid postId, Guid authorId, string content, Guid? parentCommentId = null)
        {
            // 验证帖子是否存在
            var post = await _forumRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new ArgumentException("帖子不存在");

            // 验证内容长度
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                throw new ArgumentException("评论内容长度必须在1-2000字符之间");

            // 如果是回复评论，验证父评论是否存在
            if (parentCommentId.HasValue)
            {
                var parentComment = await _forumRepository.GetCommentByIdAsync(parentCommentId.Value);
                if (parentComment == null || parentComment.PostId != postId)
                    throw new ArgumentException("父评论不存在或不属于该帖子");
            }

            var comment = new Comment(postId, authorId, content.Trim(), parentCommentId);
            var createdComment = await _forumRepository.CreateCommentAsync(comment);
            
            // 更新帖子的评论数
            post.AddComment(createdComment);
            await _forumRepository.UpdatePostAsync(post);

            return createdComment;
        }

        /// <summary>
        /// 编辑评论
        /// </summary>
        public async Task<Comment> EditCommentAsync(Guid commentId, Guid userId, string content)
        {
            var comment = await _forumRepository.GetCommentByIdAsync(commentId);
            if (comment == null)
                throw new ArgumentException("评论不存在");

            if (comment.AuthorId != userId)
                throw new UnauthorizedAccessException("只能编辑自己的评论");

            // 验证内容长度
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                throw new ArgumentException("评论内容长度必须在1-2000字符之间");

            comment.EditComment(content.Trim());
            return await _forumRepository.UpdateCommentAsync(comment);
        }

        /// <summary>
        /// 删除评论
        /// </summary>
        public async Task DeleteCommentAsync(Guid commentId, Guid userId)
        {
            var comment = await _forumRepository.GetCommentByIdAsync(commentId);
            if (comment == null)
                throw new ArgumentException("评论不存在");

            if (comment.AuthorId != userId)
                throw new UnauthorizedAccessException("只能删除自己的评论");

            comment.DeleteComment();
            await _forumRepository.UpdateCommentAsync(comment);
        }

        /// <summary>
        /// 点赞帖子
        /// </summary>
        public async Task TogglePostLikeAsync(Guid postId, Guid userId)
        {
            // 检查帖子是否存在
            var post = await _forumRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new ArgumentException("帖子不存在");

            // 直接在 Repository 层操作，不通过领域实体的私有集合
            await _forumRepository.TogglePostLikeAsync(postId, userId);
        }

        /// <summary>
        /// 收藏帖子
        /// </summary>
        public async Task TogglePostFavoriteAsync(Guid postId, Guid userId)
        {
            // 检查帖子是否存在
            var post = await _forumRepository.GetPostByIdAsync(postId);
            if (post == null)
                throw new ArgumentException("帖子不存在");

            // 直接在 Repository 层操作，不通过领域实体的私有集合
            await _forumRepository.TogglePostFavoriteAsync(postId, userId);
        }
    }
}
