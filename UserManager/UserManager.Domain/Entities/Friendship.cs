using System;

namespace UserManager.Domain.Entities
{
    public class Friendship
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid FriendUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        private Friendship() { }

        public Friendship(Guid userId, Guid friendUserId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            FriendUserId = friendUserId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}


