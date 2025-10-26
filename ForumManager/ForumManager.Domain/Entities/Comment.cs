namespace ForumManager.Domain.Entities
{
    /// <summary>
    /// 评论实体
    /// </summary>
    public class Comment : IAggregateRoot
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; } // 支持回复评论
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int LikeCount { get; set; }

        private readonly List<CommentLike> _likes = new();
        public IReadOnlyCollection<CommentLike> Likes => _likes.AsReadOnly();

        private Comment() { }

        public Comment(Guid postId, Guid authorId, string content, Guid? parentCommentId = null)
        {
            Id = Guid.NewGuid();
            PostId = postId;
            AuthorId = authorId;
            Content = content;
            ParentCommentId = parentCommentId;
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
            LikeCount = 0;
        }

        /// <summary>
        /// 编辑评论
        /// </summary>
        public void EditComment(string content)
        {
            if (IsDeleted)
                throw new InvalidOperationException("无法编辑已删除的评论");

            Content = content;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 删除评论
        /// </summary>
        public void DeleteComment()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 点赞评论
        /// </summary>
        public void LikeComment(Guid userId)
        {
            if (_likes.Any(l => l.UserId == userId))
                return; // 已经点赞过了

            _likes.Add(new CommentLike(Id, userId));
            LikeCount++;
        }

        /// <summary>
        /// 取消点赞
        /// </summary>
        public void UnlikeComment(Guid userId)
        {
            var like = _likes.FirstOrDefault(l => l.UserId == userId);
            if (like != null)
            {
                _likes.Remove(like);
                LikeCount--;
            }
        }
    }
}
