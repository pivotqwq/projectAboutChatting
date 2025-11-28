using System;

namespace UserManager.Domain.Entities
{
    public class GroupMember
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "member"; // owner/admin/member
        public string? Nickname { get; set; } // 群内昵称（可为空，默认使用全局用户名）
        public DateTime JoinedAt { get; set; }

        private GroupMember() { }

        public GroupMember(Guid groupId, Guid userId, string role = "member")
        {
            Id = Guid.NewGuid();
            GroupId = groupId;
            UserId = userId;
            Role = role;
            JoinedAt = DateTime.UtcNow;
        }
    }
}


