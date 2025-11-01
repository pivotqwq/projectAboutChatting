using System;

namespace UserManager.Domain.Entities
{
    public class GroupMember
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "member"; // owner/admin/member
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


