namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 邮箱验证码登录请求
    /// </summary>
    public class LoginByEmailCodeRequest
    {
        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// 验证码
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}

