using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ForumManager.Infrastructure;
using ForumManager.Domain;
using ForumManager.WebAPI.Events;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using ForumManager.Domain.Events;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "论坛管理API", 
        Version = "v1",
        Description = "提供帖子发布、评论、点赞、收藏等功能"
    });
    
    // 添加JWT认证支持
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT授权(数据将在请求头中进行传输) 直接在下框中输入Bearer {token}（注意两者之间是一个空格）",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference 
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// 配置数据库
builder.Services.AddDbContext<ForumDBContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ForumConnection"),
        npgsqlOptionsAction: sql => sql.MigrationsAssembly("ForumManager.Infrastructure")
    );
});

// 配置Redis缓存（可选）
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "ForumManager:";
        });
    }
    catch
    {
        // 如果Redis连接失败，使用内存缓存作为后备
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    // 如果没有Redis配置，使用内存缓存
    builder.Services.AddDistributedMemoryCache();
}

// 配置MediatR
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PostCreatedEvent).Assembly);
});

// 注册服务
builder.Services.AddScoped<ForumDomainService>();
builder.Services.AddScoped<IForumRepository, ForumRepository>();
builder.Services.AddScoped<RedisForumCache>();

// 配置JWT认证
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey未配置");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ForumManagerAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ForumManagerClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 配置API行为
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Allow47", policy =>
    {
        policy.WithOrigins("http://47.99.79.0")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Allow47");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 数据库迁移配置（生产环境规范实现）
await ApplyDatabaseMigrationsAsync(app);

app.Run();

/// <summary>
/// 应用数据库迁移（生产环境规范实现）
/// </summary>
static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    // 检查是否启用自动迁移
    var runMigrations = app.Configuration.GetValue<bool>("Database:RunMigrationsOnStartup", defaultValue: true);
    if (!runMigrations)
    {
        var earlyLogger = app.Services.GetRequiredService<ILogger<Program>>();
        earlyLogger.LogInformation("数据库自动迁移已禁用，跳过迁移步骤");
        return;
    }

    using var scope = app.Services.CreateScope();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<Program>();

    try
    {
        logger.LogInformation("开始执行数据库迁移...");

        var context = scope.ServiceProvider.GetRequiredService<ForumDBContext>();
        var database = context.Database;

        // 检查数据库连接
        if (!await database.CanConnectAsync())
        {
            logger.LogError("无法连接到数据库，请检查连接字符串配置");
            return;
        }

        logger.LogInformation("数据库连接成功");

        // 检查是否有待应用的迁移
        var pendingMigrations = await database.GetPendingMigrationsAsync();
        var pendingMigrationsList = pendingMigrations.ToList();

        if (pendingMigrationsList.Count == 0)
        {
            logger.LogInformation("数据库已是最新版本，无需迁移");
            return;
        }

        logger.LogInformation("发现 {Count} 个待应用的迁移: {Migrations}",
            pendingMigrationsList.Count,
            string.Join(", ", pendingMigrationsList));

        // 执行迁移（带重试机制）
        const int maxRetries = 3;
        const int retryDelaySeconds = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await database.MigrateAsync();
                logger.LogInformation("数据库迁移成功完成");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && IsRetryableException(ex))
            {
                logger.LogWarning(ex, "迁移尝试 {Attempt}/{MaxRetries} 失败，{Delay}秒后重试...",
                    attempt, maxRetries, retryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
        }

        // 所有重试都失败
        throw new InvalidOperationException($"数据库迁移在 {maxRetries} 次尝试后仍然失败");
    }
    catch (Npgsql.PostgresException pgEx)
    {
        // PostgreSQL 特定错误处理
        logger.LogError(pgEx,
            "数据库迁移失败 - PostgreSQL错误: {SqlState} - {MessageText}. " +
            "请检查数据库权限（需要USAGE和CREATE权限）和迁移历史表访问权限。",
            pgEx.SqlState, pgEx.MessageText);
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "数据库迁移失败。错误详情: {ErrorMessage}. " +
            "请检查数据库连接字符串、网络连接和数据库权限。",
            ex.Message);
    }

    // 生产环境：迁移失败时可以选择停止服务或继续运行
    var stopOnMigrationFailure = app.Configuration.GetValue<bool>("Database:StopOnMigrationFailure", defaultValue: false);
    if (stopOnMigrationFailure)
    {
        logger.LogCritical("由于迁移失败且 StopOnMigrationFailure=true，应用将退出");
        Environment.Exit(1);
    }
    else
    {
        logger.LogWarning(
            "数据库迁移失败，但应用将继续运行。请尽快手动解决迁移问题，" +
            "否则数据库操作可能会失败。");
    }
}

/// <summary>
/// 判断异常是否可重试
/// </summary>
static bool IsRetryableException(Exception ex)
{
    // 网络错误、连接超时等可重试的错误
    return ex is TimeoutException
        || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
}
