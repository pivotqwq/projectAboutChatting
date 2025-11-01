using System;

namespace UserManager.Domain.Entities
{
    /// <summary>
    /// 好友请求实体
    /// </summary>
    public class FriendRequest
    {
        public Guid Id { get; set; }
        public Guid RequesterId { get; set; }  // 请求者ID
        public Guid ReceiverId { get; set; }    // 接收者ID
        public string Status { get; set; } = "pending";  // pending/accepted/rejected
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        private FriendRequest() { }

        public FriendRequest(Guid requesterId, Guid receiverId)
        {
            Id = Guid.NewGuid();
            RequesterId = requesterId;
            ReceiverId = receiverId;
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

