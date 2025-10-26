using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using UserManager.Domain;
using UserManager.Domain.ValueObjects;

namespace UserManager.Infrastracture
{
    /// <summary>
    /// 基于Redis的手机验证码缓存实现
    /// </summary>
    public class RedisPhoneCodeCache : IPhoneCodeCache
    {
        private readonly IDistributedCache _cache;

        public RedisPhoneCodeCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SaveCodeAsync(UserBasic userBasic, string code, TimeSpan expiration)
        {
            string key = GetCacheKey(userBasic);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _cache.SetStringAsync(key, code, options);
        }

        public async Task<string?> RetrieveCodeAsync(UserBasic userBasic)
        {
            string key = GetCacheKey(userBasic);
            return await _cache.GetStringAsync(key);
        }

        public async Task RemoveCodeAsync(UserBasic userBasic)
        {
            string key = GetCacheKey(userBasic);
            await _cache.RemoveAsync(key);
        }

        private string GetCacheKey(UserBasic userBasic)
        {
            // 使用手机号和邮箱组合作为缓存键
            return $"PhoneCode:{userBasic.PhoneNumber}:{userBasic.Email}";
        }
    }
}

