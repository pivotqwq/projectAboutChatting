using MediatR;

namespace ForumManager.Domain.Events
{
    /// <summary>
    /// 评论添加事件
    /// </summary>
    public class CommentAddedEvent : INotification
    {
        public Guid CommentId { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }

        public CommentAddedEvent()
        {
        }

        public CommentAddedEvent(Entities.Comment comment)
        {
            CommentId = comment.Id;
            PostId = comment.PostId;
            AuthorId = comment.AuthorId;
            Content = comment.Content;
            ParentCommentId = comment.ParentCommentId;
            CreatedAt = comment.CreatedAt;
        }
    }
}
