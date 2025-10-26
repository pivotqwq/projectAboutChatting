namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 发送邮箱验证码请求
    /// </summary>
    public class SendEmailCodeRequest
    {
        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}

