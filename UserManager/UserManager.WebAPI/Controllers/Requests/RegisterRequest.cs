using UserManager.Domain.ValueObjects;

namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 用户注册请求模型
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// 用户基本信息
        /// </summary>
        public UserBasic UserBasic { get; set; }
        
        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// 邮箱验证码
        /// </summary>
        public string EmailCode { get; set; } = string.Empty;
    }
}