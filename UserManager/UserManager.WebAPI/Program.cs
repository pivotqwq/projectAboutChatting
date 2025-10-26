using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;
using UserManager.Infrastracture;
using MediatR;
using Npgsql.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManager.Domain;
using UserManager.WebAPI;
using UserManager.WebAPI.Services;
using UserManager.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "用户管理API", 
        Version = "v1",
        Description = "提供用户注册、登录、管理等功能"
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
builder.Services.AddDbContext<UserDBContext>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 配置Redis缓存
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "UserManager:";
});

builder.Services.Configure<MvcOptions>(o =>
{
    o.Filters.Add<UnitOfWorkFilter>();
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddScoped<UserDomainService>();
builder.Services.AddScoped<IUserRepository,UserRepository>();
builder.Services.AddScoped<IUserProfileRepository,UserProfileRepository>();
builder.Services.AddScoped<ISmsCodeSender,SmsCodeSender>();
builder.Services.AddScoped<IPhoneCodeCache,RedisPhoneCodeCache>();
builder.Services.AddScoped<IEmailCodeSender,EmailCodeSender>();
builder.Services.AddScoped<IEmailCodeCache,RedisEmailCodeCache>();
builder.Services.AddScoped<JwtTokenService>();

// 配置JWT认证
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey未配置");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "UserManagerAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "UserManagerClient";

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
        ClockSkew = TimeSpan.Zero // 移除默认的5分钟时间偏移
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 添加管理员权限验证中间件
app.UseMiddleware<AdminAuthorizationMiddleware>();

app.MapControllers();

// 确保数据库迁移
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserDBContext>();
    try
    {
        // 自动应用待处理的迁移
        context.Database.Migrate();
        Console.WriteLine("✅ 数据库迁移已自动应用");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 数据库迁移失败: {ex.Message}");
        // 在生产环境中，你可能想要记录错误而不是抛出异常
        // throw;
    }
}

app.Run();
