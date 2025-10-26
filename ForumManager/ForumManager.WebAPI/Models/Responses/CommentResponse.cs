namespace ForumManager.WebAPI.Models.Responses
{
    /// <summary>
    /// 评论响应模型
    /// </summary>
    public class CommentResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public Guid? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int LikeCount { get; set; }
    }
}
