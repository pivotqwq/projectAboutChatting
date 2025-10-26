namespace UserManager.Domain
{
    /// <summary>
    /// 邮箱验证码缓存接口
    /// </summary>
    public interface IEmailCodeCache
    {
        /// <summary>
        /// 保存验证码
        /// </summary>
        Task SaveCodeAsync(string email, string code, TimeSpan expiration);

        /// <summary>
        /// 获取验证码
        /// </summary>
        Task<string?> RetrieveCodeAsync(string email);

        /// <summary>
        /// 删除验证码
        /// </summary>
        Task RemoveCodeAsync(string email);
    }
}

