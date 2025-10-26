using UserManager.Domain.ValueObjects;

namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 登录请求模型
    /// </summary>
    public class LoginByPhoneAndPwdRequest
    {
        /// <summary>
        /// 用户基本信息
        /// </summary>
        public UserBasic UserBasic { get; set; }
        
        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password { get; set; }
    }
}