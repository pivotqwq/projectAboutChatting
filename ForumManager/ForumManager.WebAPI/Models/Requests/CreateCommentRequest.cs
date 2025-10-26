using System.ComponentModel.DataAnnotations;

namespace ForumManager.WebAPI.Models.Requests
{
    /// <summary>
    /// 创建评论请求
    /// </summary>
    public class CreateCommentRequest
    {
        /// <summary>
        /// 帖子ID
        /// </summary>
        [Required(ErrorMessage = "帖子ID不能为空")]
        public Guid PostId { get; set; }

        /// <summary>
        /// 评论内容
        /// </summary>
        [Required(ErrorMessage = "评论内容不能为空")]
        [StringLength(2000, ErrorMessage = "评论内容长度不能超过2000字符")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 父评论ID（用于回复评论）
        /// </summary>
        public Guid? ParentCommentId { get; set; }
    }
}
