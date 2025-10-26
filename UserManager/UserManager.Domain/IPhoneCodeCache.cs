using UserManager.Domain.ValueObjects;

namespace UserManager.Domain
{
    /// <summary>
    /// 手机验证码缓存接口
    /// </summary>
    public interface IPhoneCodeCache
    {
        /// <summary>
        /// 保存验证码
        /// </summary>
        Task SaveCodeAsync(UserBasic userBasic, string code, TimeSpan expiration);

        /// <summary>
        /// 获取验证码
        /// </summary>
        Task<string?> RetrieveCodeAsync(UserBasic userBasic);

        /// <summary>
        /// 删除验证码
        /// </summary>
        Task RemoveCodeAsync(UserBasic userBasic);
    }
}

