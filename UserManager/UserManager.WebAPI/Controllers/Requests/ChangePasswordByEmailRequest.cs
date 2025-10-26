namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 通过邮箱验证码修改密码请求
    /// </summary>
    public class ChangePasswordByEmailRequest
    {
        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// 邮箱验证码
        /// </summary>
        public string EmailCode { get; set; } = string.Empty;
        
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
