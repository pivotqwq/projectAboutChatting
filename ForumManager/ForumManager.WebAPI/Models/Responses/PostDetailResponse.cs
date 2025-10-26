using ForumManager.Domain.ValueObjects;

namespace ForumManager.WebAPI.Models.Responses
{
    /// <summary>
    /// 帖子详情响应模型
    /// </summary>
    public class PostDetailResponse : PostResponse
    {
        public List<CommentResponse> Comments { get; set; } = new();
    }
}
