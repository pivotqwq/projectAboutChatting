using UserManager.Domain.ValueObjects;

namespace UserManager.Domain
{
    /// <summary>
    /// 邮箱验证码发送接口
    /// </summary>
    public interface IEmailCodeSender
    {
        /// <summary>
        /// 发送邮箱验证码
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <param name="code">验证码</param>
        Task SendCodeAsync(string email, string code);
    }
}

