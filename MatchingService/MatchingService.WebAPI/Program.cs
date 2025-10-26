using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;
using MatchingService.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.IRepositories;
using MatchingService.Infrastructure.Repositories;
using MatchingService.Domain.Services;
using MatchingService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MatchingService.WebAPI.Middleware;
using MatchingService.WebAPI.Attributes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelAttribute>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "匹配服务API", 
        Version = "v1",
        Description = "提供标签管理、用户匹配、智能推荐等功能"
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
builder.Services.AddDbContext<MatchingDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 配置Redis缓存（可选）
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "MatchingService:";
    });
}
else
{
    // 如果没有Redis配置，使用内存缓存作为备选
    builder.Services.AddMemoryCache();
}

// 配置MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// 注册仓储
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IUserTagRepository, UserTagRepository>();
builder.Services.AddScoped<IUserMatchRepository, UserMatchRepository>();
builder.Services.AddScoped<IUserInteractionRepository, UserInteractionRepository>();

// 注册服务
builder.Services.AddScoped<MatchingDomainService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// 根据Redis配置注册不同的缓存服务
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddScoped<ICacheService, RedisCacheService>();
}
else
{
    builder.Services.AddScoped<ICacheService, MemoryCacheService>();
}

// 配置JWT认证
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey未配置");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MatchingServiceAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MatchingServiceClient";

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

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
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

app.UseCors("AllowAll");

// 添加错误处理中间件
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 确保数据库创建
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MatchingDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
