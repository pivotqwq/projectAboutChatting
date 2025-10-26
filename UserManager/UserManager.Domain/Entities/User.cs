using UserManager.Domain.ValueObjects;
using Zack.Commons;
using System.Linq;

namespace UserManager.Domain.Entities
{
    public record User : IAggregateRoot
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public UserBasic UserBasic { get; set; }
        private string? passwordHash;
        public UserAccessFail UserAccessFail { get; private set; }
        private User()
        {

        }
        public User(UserBasic userBasic)
        {
            this.Id = Guid.NewGuid();
            this.UserAccessFail = new UserAccessFail(this);
            UserBasic = userBasic;
            // 自动生成姓名：用户+随机码（6位数字）
            this.Name = GenerateRandomUserName();
        }
        
        /// <summary>
        /// 生成随机用户名称
        /// </summary>
        /// <returns>格式为"用户+6位随机数字"的用户名</returns>
        private string GenerateRandomUserName()
        {
            // 使用随机数生成6位字母和数字混合码
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var randomCode = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return "用户" + randomCode;
        }
        public bool HasPassword()
        {
            return !string.IsNullOrEmpty(this.passwordHash);
        }
        public void ChangePassword(string password)
        {
            if(password.Length <=6)
            {
                throw new ArgumentOutOfRangeException("密码长度必须大于6位");
            }
            this.passwordHash = HashHelper.ComputeMd5Hash(password);
        }
        public bool CheckPassword(string password) 
        { 
            return this.passwordHash == HashHelper.ComputeMd5Hash(password);
        }
        public void ChangeBasic(UserBasic userBasic)
        {
            this.UserBasic = userBasic;
        }
    }
}
