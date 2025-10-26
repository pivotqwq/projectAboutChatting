using ForumManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ForumManager.Infrastructure
{
    /// <summary>
    /// 论坛数据库上下文
    /// </summary>
    public class ForumDBContext : DbContext
    {
        public ForumDBContext(DbContextOptions<ForumDBContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostFavorite> PostFavorites { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置Post实体
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Content).HasMaxLength(10000).IsRequired();
                entity.Property(e => e.Category).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.Tags).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
                );
                entity.Property(e => e.ViewCount).HasDefaultValue(0);
                entity.Property(e => e.LikeCount).HasDefaultValue(0);
                entity.Property(e => e.CommentCount).HasDefaultValue(0);
                entity.Property(e => e.FavoriteCount).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 配置索引
                entity.HasIndex(e => e.AuthorId);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ViewCount);
                entity.HasIndex(e => e.LikeCount);
            });

            // 配置Comment实体
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.LikeCount).HasDefaultValue(0);

                // 配置索引
                entity.HasIndex(e => e.PostId);
                entity.HasIndex(e => e.AuthorId);
                entity.HasIndex(e => e.ParentCommentId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsDeleted);

                // 配置外键关系
                entity.HasOne<Post>()
                      .WithMany(p => p.Comments)
                      .HasForeignKey(c => c.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置PostLike实体
            modelBuilder.Entity<PostLike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 配置索引
                entity.HasIndex(e => e.PostId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique();

                // 配置外键关系
                entity.HasOne<Post>()
                      .WithMany(p => p.Likes)
                      .HasForeignKey(pl => pl.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置PostFavorite实体
            modelBuilder.Entity<PostFavorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 配置索引
                entity.HasIndex(e => e.PostId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique();

                // 配置外键关系
                entity.HasOne<Post>()
                      .WithMany(p => p.Favorites)
                      .HasForeignKey(pf => pf.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置CommentLike实体
            modelBuilder.Entity<CommentLike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 配置索引
                entity.HasIndex(e => e.CommentId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.CommentId, e.UserId }).IsUnique();

                // 配置外键关系
                entity.HasOne<Comment>()
                      .WithMany(c => c.Likes)
                      .HasForeignKey(cl => cl.CommentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
