using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture
{
    public class UserDBContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }
        
        // 用于运行时的构造函数
        public UserDBContext(DbContextOptions<UserDBContext> opt) : base(opt) 
        {
            
        }
        
        // 用于设计时的无参构造函数（EF Core迁移工具使用）
        public UserDBContext() { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
        
        // 设计时配置（仅在没有外部配置时使用）
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 此连接字符串仅用于设计时迁移操作
                // 运行时连接字符串从appsettings.json读取
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=UserManagerDB;Username=postgres;Password=123456;");
            }
        }
    }
}
