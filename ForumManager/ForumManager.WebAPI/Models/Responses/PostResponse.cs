using ForumManager.Domain.ValueObjects;

namespace ForumManager.WebAPI.Models.Responses
{
    /// <summary>
    /// 帖子响应模型
    /// </summary>
    public class PostResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? TitleImageBase64 { get; set; }
        public PostCategory Category { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int FavoriteCount { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
