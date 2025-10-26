namespace ForumManager.Domain.Entities
{
    /// <summary>
    /// 评论点赞实体
    /// </summary>
    public class CommentLike : IAggregateRoot
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        private CommentLike() { }

        public CommentLike(Guid commentId, Guid userId)
        {
            Id = Guid.NewGuid();
            CommentId = commentId;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
