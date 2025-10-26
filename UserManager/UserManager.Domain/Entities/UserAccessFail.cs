using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager.Domain.Entities
{
    public record UserAccessFail
    {
        public Guid Id { get; init; }
        public User User { get; init; }
        public Guid UserId { get; init; }
        public bool IsLockedOut { get; private set; }
        public DateTime? LockEnd { get; private set; }
        public int AccessFailCount {  get; private set; }
        private UserAccessFail() { }
        public UserAccessFail(User user)
        {
            this.Id = Guid.NewGuid();
            this.User = user;
        }
        public void Reset()
        {
            this.AccessFailCount = 0;
            this.LockEnd = null;
            this.IsLockedOut = false;
        }
        public void Fail()
        {
            this.AccessFailCount++;
            if (this.AccessFailCount >= 5)
            {
                this.LockEnd = DateTime.UtcNow.AddMinutes(5);
                this.IsLockedOut = true;
            }
        }

        public bool IsLockOut()
        {
            if (this.IsLockedOut)
            {
                if (DateTime.UtcNow > this.LockEnd)
                {
                    Reset();
                    return false;
                }
                else { return true; }
            }
            else 
            { 
                return false;   
            }
        }

        /// <summary>
        /// 锁定用户到指定时间
        /// </summary>
        public void LockUntil(DateTime lockUntil)
        {
            this.IsLockedOut = true;
            this.LockEnd = lockUntil;
        }

        /// <summary>
        /// 永久锁定用户
        /// </summary>
        public void LockPermanently()
        {
            this.IsLockedOut = true;
            this.LockEnd = DateTime.MaxValue;
        }
    }
}
