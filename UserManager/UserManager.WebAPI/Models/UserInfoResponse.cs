using UserManager.Domain.ValueObjects;

namespace UserManager.WebAPI.Models
{
    /// <summary>
    /// 用户信息响应
    /// </summary>
    public class UserInfoResponse
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 用户基本信息
        /// </summary>
        public UserBasicDto UserBasic { get; set; } = new();

        /// <summary>
        /// 用户个人资料
        /// </summary>
        public UserProfileDto? UserProfile { get; set; }
    }

    /// <summary>
    /// 用户基本信息DTO
    /// </summary>
    public class UserBasicDto
    {
        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }
    }

    /// <summary>
    /// 用户个人资料DTO
    /// </summary>
    public class UserProfileDto
    {
        /// <summary>
        /// 头像Base64编码
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        public string? RealName { get; set; }

        /// <summary>
        /// 性别
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
        /// 个人简介
        /// </summary>
        public string? Bio { get; set; }
    }
}

