using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;
using System.Reflection;

namespace MatchingService.Infrastructure
{
    /// <summary>
    /// 匹配服务数据库上下文
    /// </summary>
    public class MatchingDbContext : DbContext
    {
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<UserMatch> UserMatches { get; set; }
        public DbSet<UserInteraction> UserInteractions { get; set; }

        // 用于运行时的构造函数
        public MatchingDbContext(DbContextOptions<MatchingDbContext> options) : base(options)
        {
        }

        // 用于设计时的无参构造函数（EF Core迁移工具使用）
        public MatchingDbContext() { }

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
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=MatchingDB;Username=postgres;Password=123456;");
            }
        }
    }
}
