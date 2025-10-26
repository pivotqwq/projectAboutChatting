using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Entities
{
    /// <summary>
    /// 用户个人信息实体
    /// </summary>
    public record UserProfile : IAggregateRoot
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        
        /// <summary>
        /// 头像Base64编码
        /// </summary>
        public string? Avatar { get; set; }
        
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string? RealName { get; set; }
        
        /// <summary>
        /// 性别 (Male/Female/Other)
        /// </summary>
        public string? Gender { get; set; }
        
        /// <summary>
        /// 生日
        /// </summary>
        public DateTime? Birthday { get; set; }
        
        /// <summary>
        /// 地区
        /// </summary>
        public string? Region { get; set; }
        
        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// 二维码图片URL
        /// </summary>
        public string? QRCodeImage { get; set; }
        
        /// <summary>
        /// 个人简介
        /// </summary>
        public string? Bio { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        private UserProfile() { }

        public UserProfile(Guid userId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新个人信息
        /// </summary>
        public void UpdateProfile(string? avatar, string? realName, string? gender, 
            DateTime? birthday, string? region, string? phoneNumber, string? bio)
        {
            Avatar = avatar;
            RealName = realName;
            Gender = gender;
            Birthday = birthday;
            Region = region;
            PhoneNumber = phoneNumber;
            Bio = bio;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
