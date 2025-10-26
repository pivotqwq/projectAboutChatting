namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 更新用户个人信息请求
    /// </summary>
    public class UpdateUserProfileRequest
    {
        /// <summary>
        /// 头像Base64编码
        /// </summary>
        public string? Avatar { get; set; }
        
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string? RealName { get; set; }
        
        /// <summary>
        /// 性别 (Male/Female/Other)
        /// </summary>
        public string? Gender { get; set; }
        
        /// <summary>
        /// 生日
        /// </summary>
        public DateTime? Birthday { get; set; }
        
        /// <summary>
        /// 地区
        /// </summary>
        public string? Region { get; set; }
        
        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }
        
        
        /// <summary>
        /// 个人简介
        /// </summary>
        public string? Bio { get; set; }
    }
}
