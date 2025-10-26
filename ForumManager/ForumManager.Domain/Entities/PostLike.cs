namespace ForumManager.Domain.Entities
{
    /// <summary>
    /// 帖子点赞实体
    /// </summary>
    public class PostLike : IAggregateRoot
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        private PostLike() { }

        public PostLike(Guid postId, Guid userId)
        {
            Id = Guid.NewGuid();
            PostId = postId;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
