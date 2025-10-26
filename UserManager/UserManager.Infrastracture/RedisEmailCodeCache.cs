using Microsoft.Extensions.Caching.Distributed;
using UserManager.Domain;

namespace UserManager.Infrastracture
{
    /// <summary>
    /// 基于Redis的邮箱验证码缓存实现
    /// </summary>
    public class RedisEmailCodeCache : IEmailCodeCache
    {
        private readonly IDistributedCache _cache;

        public RedisEmailCodeCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SaveCodeAsync(string email, string code, TimeSpan expiration)
        {
            string key = GetCacheKey(email);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _cache.SetStringAsync(key, code, options);
        }

        public async Task<string?> RetrieveCodeAsync(string email)
        {
            string key = GetCacheKey(email);
            return await _cache.GetStringAsync(key);
        }

        public async Task RemoveCodeAsync(string email)
        {
            string key = GetCacheKey(email);
            await _cache.RemoveAsync(key);
        }

        private string GetCacheKey(string email)
        {
            return $"EmailCode:{email}";
        }
    }
}

