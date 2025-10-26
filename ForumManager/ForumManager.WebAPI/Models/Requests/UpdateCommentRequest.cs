using System.ComponentModel.DataAnnotations;

namespace ForumManager.WebAPI.Models.Requests
{
    /// <summary>
    /// 更新评论请求
    /// </summary>
    public class UpdateCommentRequest
    {
        /// <summary>
        /// 评论内容
        /// </summary>
        [Required(ErrorMessage = "评论内容不能为空")]
        [StringLength(2000, ErrorMessage = "评论内容长度不能超过2000字符")]
        public string Content { get; set; } = string.Empty;
    }
}
