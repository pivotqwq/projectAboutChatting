using MediatR;

namespace ForumManager.Domain.Events
{
    /// <summary>
    /// 帖子创建事件
    /// </summary>
    public class PostCreatedEvent : INotification
    {
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ValueObjects.PostCategory Category { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        public PostCreatedEvent()
        {
        }

        public PostCreatedEvent(Entities.Post post)
        {
            PostId = post.Id;
            AuthorId = post.AuthorId;
            Title = post.Title;
            Content = post.Content;
            Category = post.Category;
            Tags = post.Tags;
            CreatedAt = post.CreatedAt;
        }
    }
}
