using System;

namespace UserManager.Domain.Entities
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }

        private Group() { }

        public Group(string name, Guid ownerId)
        {
            Id = Guid.NewGuid();
            Name = name;
            OwnerId = ownerId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}


