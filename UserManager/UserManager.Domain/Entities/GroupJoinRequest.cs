using System;

namespace UserManager.Domain.Entities
{
    /// <summary>
    /// 群聊加入申请实体
    /// </summary>
    public class GroupJoinRequest
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid RequesterId { get; set; }   // 申请者ID
        public string Status { get; set; } = "pending";  // pending/accepted/rejected
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        private GroupJoinRequest() { }

        public GroupJoinRequest(Guid groupId, Guid requesterId)
        {
            Id = Guid.NewGuid();
            GroupId = groupId;
            RequesterId = requesterId;
            Status = "pending";
            CreatedAt = DateTime.UtcNow;
        }

        public void Accept()
        {
            Status = "accepted";
            RespondedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = "rejected";
            RespondedAt = DateTime.UtcNow;
        }
    }
}

