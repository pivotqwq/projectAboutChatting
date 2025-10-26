namespace ForumManager.Domain.Entities
{
    /// <summary>
    /// 帖子收藏实体
    /// </summary>
    public class PostFavorite : IAggregateRoot
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        private PostFavorite() { }

        public PostFavorite(Guid postId, Guid userId)
        {
            Id = Guid.NewGuid();
            PostId = postId;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
