using System.ComponentModel.DataAnnotations;

namespace ForumManager.WebAPI.Models.Requests
{
    /// <summary>
    /// 更新帖子请求
    /// </summary>
    public class UpdatePostRequest
    {
        /// <summary>
        /// 帖子标题
        /// </summary>
        [Required(ErrorMessage = "标题不能为空")]
        [StringLength(200, ErrorMessage = "标题长度不能超过200字符")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 帖子内容
        /// </summary>
        [Required(ErrorMessage = "内容不能为空")]
        [StringLength(10000, ErrorMessage = "内容长度不能超过10000字符")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string>? Tags { get; set; }
    }
}
