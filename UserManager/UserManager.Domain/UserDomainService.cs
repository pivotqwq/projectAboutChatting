using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;
using UserManager.Domain.Events;

namespace UserManager.Domain
{
    public class UserDomainService
    {
        private readonly IUserRepository repository;
        private readonly ISmsCodeSender smsSender;
        private readonly IPhoneCodeCache phoneCodeCache;
        private readonly IEmailCodeSender? emailSender;
        private readonly IEmailCodeCache? emailCodeCache;

        public UserDomainService(
            IUserRepository repository, 
            ISmsCodeSender smsSender, 
            IPhoneCodeCache phoneCodeCache,
            IEmailCodeSender? emailSender = null,
            IEmailCodeCache? emailCodeCache = null)
        {
            this.repository = repository;
            this.smsSender = smsSender;
            this.phoneCodeCache = phoneCodeCache;
            this.emailSender = emailSender;
            this.emailCodeCache = emailCodeCache;
        }

        public async Task<UserAccessResult> CheckLoginAsync(UserBasic userBasic,
            string password)
        {
            User? user = await repository.FindOneAsync(userBasic);
            UserAccessResult result;
            if (user == null)//找不到用户
            {
                result = UserAccessResult.PhoneNumberNotFound;
            }
            else if (IsLockOut(user))//用户被锁定
            {
                result = UserAccessResult.Lockout;
            }
            else if(user.HasPassword()==false)//没设密码
            {
                result = UserAccessResult.NoPassword;
            }
            else if(user.CheckPassword(password))//密码正确
            {
                result = UserAccessResult.OK;
            }
            else//密码错误
            {
                result = UserAccessResult.PasswordError;
            }
            if(user!=null)
            {
                if (result == UserAccessResult.OK)
                {
                    this.ResetAccessFail(user);//重置
                }
                else
                {
                    this.AccessFail(user);//处理登录失败
                }
            }            
            UserAccessResultEvent eventItem = new(userBasic, result);
            await repository.PublishEventAsync(eventItem);
            return result;
        }

        public async Task<UserAccessResult> SendCodeAsync(UserBasic userBasic)
        {
            var user = await repository.FindOneAsync(userBasic);
            if (user == null)
            {
                return UserAccessResult.PhoneNumberNotFound;
            }
            if (IsLockOut(user))
            {
                return UserAccessResult.Lockout;
            }
            string code = Random.Shared.Next(1000, 9999).ToString();
            // 验证码5分钟过期
            await phoneCodeCache.SaveCodeAsync(userBasic, code, TimeSpan.FromMinutes(5));
            await smsSender.SendCodeAsync(userBasic, code);
            return UserAccessResult.OK;
        }

        public async Task<CheckCodeResult> CheckCodeAsync(UserBasic userBasic,string code)
        {
            var user = await repository.FindOneAsync(userBasic);
            if (user == null)
            {
                return CheckCodeResult.PhoneNumberNotFound;
            }
            if (IsLockOut(user))
            {
                return CheckCodeResult.Lockout;
            }
            string? codeInServer = await phoneCodeCache.RetrieveCodeAsync(userBasic);
            if (string.IsNullOrEmpty(codeInServer))
            {
                return CheckCodeResult.CodeError;
            }
            if (code == codeInServer)
            {
                // 验证成功后删除验证码
                await phoneCodeCache.RemoveCodeAsync(userBasic);
                return CheckCodeResult.OK;
            }
            else
            {
                AccessFail(user);
                return CheckCodeResult.CodeError;
            }
        }

        public void ResetAccessFail(User user)
        {
            user.UserAccessFail.Reset();
        }

        public bool IsLockOut(User user)
        {
            return user.UserAccessFail.IsLockOut();
        }

        public void AccessFail(User user)
        {
            user.UserAccessFail.Fail();
        }

        /// <summary>
        /// 发送邮箱验证码
        /// </summary>
        public async Task<UserAccessResult> SendEmailCodeAsync(string email)
        {
            if (emailSender == null || emailCodeCache == null)
            {
                throw new InvalidOperationException("邮箱验证码服务未配置");
            }

            // 根据邮箱查找用户
            var userBasic = new UserBasic(null,email);
            var user = await repository.FindOneAsync(userBasic);
            if (user == null)
            {
                return UserAccessResult.PhoneNumberNotFound;
            }
            if (IsLockOut(user))
            {
                return UserAccessResult.Lockout;
            }

            string code = Random.Shared.Next(100000, 999999).ToString(); // 6位数字验证码
            // 验证码5分钟过期
            await emailCodeCache.SaveCodeAsync(email, code, TimeSpan.FromMinutes(5));
            await emailSender.SendCodeAsync(email, code);
            return UserAccessResult.OK;
        }

        /// <summary>
        /// 验证邮箱验证码
        /// </summary>
        public async Task<CheckCodeResult> CheckEmailCodeAsync(string email, string code)
        {
            if (emailCodeCache == null)
            {
                throw new InvalidOperationException("邮箱验证码服务未配置");
            }

            var userBasic = new UserBasic(null, email);
            var user = await repository.FindOneAsync(userBasic);
            if (user == null)
            {
                return CheckCodeResult.PhoneNumberNotFound;
            }
            if (IsLockOut(user))
            {
                return CheckCodeResult.Lockout;
            }

            string? codeInServer = await emailCodeCache.RetrieveCodeAsync(email);
            if (string.IsNullOrEmpty(codeInServer))
            {
                return CheckCodeResult.CodeError;
            }
            if (code == codeInServer)
            {
                // 验证成功后删除验证码
                await emailCodeCache.RemoveCodeAsync(email);
                ResetAccessFail(user);
                return CheckCodeResult.OK;
            }
            else
            {
                AccessFail(user);
                return CheckCodeResult.CodeError;
            }
        }

        /// <summary>
        /// 发送注册邮箱验证码（不需要用户已存在）
        /// </summary>
        public async Task<bool> SendRegisterEmailCodeAsync(string email)
        {
            if (emailSender == null || emailCodeCache == null)
            {
                throw new InvalidOperationException("邮箱验证码服务未配置");
            }

            string code = Random.Shared.Next(100000, 999999).ToString(); // 6位数字验证码
            // 验证码5分钟过期
            await emailCodeCache.SaveCodeAsync(email, code, TimeSpan.FromMinutes(5));
            await emailSender.SendCodeAsync(email, code);
            return true;
        }

        /// <summary>
        /// 验证注册邮箱验证码（不需要用户已存在）
        /// </summary>
        public async Task<bool> CheckRegisterEmailCodeAsync(string email, string code)
        {
            if (emailCodeCache == null)
            {
                throw new InvalidOperationException("邮箱验证码服务未配置");
            }

            string? codeInServer = await emailCodeCache.RetrieveCodeAsync(email);
            if (string.IsNullOrEmpty(codeInServer))
            {
                return false;
            }
            if (code == codeInServer)
            {
                // 验证成功后删除验证码
                await emailCodeCache.RemoveCodeAsync(email);
                return true;
            }
            return false;
        }
    }
}
