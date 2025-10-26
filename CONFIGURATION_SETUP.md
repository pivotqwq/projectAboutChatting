# 项目配置设置指南

## 概述

本项目使用微服务架构，包含多个服务，每个服务都需要独立的配置文件。为了安全起见，所有包含敏感信息的配置文件都被排除在Git版本控制之外。

## 配置文件说明

### 被忽略的敏感文件

以下文件包含敏感信息，已被添加到 `.gitignore` 中，不会提交到Git：

- `**/appsettings.json` - 生产环境配置
- `**/appsettings.Development.json` - 开发环境配置  
- `**/appsettings.Production.json` - 生产环境配置
- `**/admin_password.txt` - 管理员密码文件
- `**/bin/` - 编译输出目录
- `**/obj/` - 编译临时文件
- `**/publish/` - 发布输出目录

### 配置模板文件

每个服务都提供了配置模板文件（`.template.json`），你需要复制这些模板并填入实际的配置值：

#### 1. API网关 (ApiGateway)
- **模板文件**: `ApiGateway/appsettings.template.json`
- **实际文件**: `ApiGateway/appsettings.json`
- **端口**: 5000 (API网关)
- **需要配置**:
  - JWT密钥 (`Jwt.Key`)
  - 服务地址（如果部署在不同服务器）

#### 2. 用户管理服务 (UserManager)
- **模板文件**: `UserManager/UserManager.WebAPI/appsettings.template.json`
- **实际文件**: `UserManager/UserManager.WebAPI/appsettings.json`
- **密码文件模板**: `UserManager/admin_password.template.txt`
- **端口**: 8081
- **需要配置**:
  - PostgreSQL数据库连接字符串
  - Redis连接字符串
  - JWT密钥
  - 邮件服务器配置（SMTP）
  - 管理员密码

#### 3. 论坛管理服务 (ForumManager)
- **模板文件**: `ForumManager/ForumManager.WebAPI/appsettings.template.json`
- **实际文件**: `ForumManager/ForumManager.WebAPI/appsettings.json`
- **端口**: 8082
- **需要配置**:
  - PostgreSQL数据库连接字符串
  - Redis连接字符串
  - JWT密钥

#### 4. 聊天服务 (ChatService)
- **模板文件**: `ChatService/appsettings.template.json`
- **实际文件**: `ChatService/appsettings.json`
- **端口**: 8083
- **需要配置**:
  - MongoDB连接字符串
  - Redis连接字符串
  - JWT密钥

#### 5. 匹配服务 (MatchingService)
- **模板文件**: `MatchingService/MatchingService.WebAPI/appsettings.template.json`
- **实际文件**: `MatchingService/MatchingService.WebAPI/appsettings.json`
- **端口**: 8084
- **需要配置**:
  - PostgreSQL数据库连接字符串
  - Redis连接字符串
  - JWT密钥

## 端口配置说明

### 默认端口分配
- **API网关**: 5000 (对外服务端口)
- **用户管理服务**: 8081
- **论坛管理服务**: 8082  
- **聊天服务**: 8083
- **匹配服务**: 8084

### 端口安全建议
1. **避免使用连续端口**: 模板文件使用8081-8084而不是9291-9294，降低被扫描的风险
2. **生产环境**: 建议使用非标准端口，如8001, 8002, 8003, 8004
3. **防火墙配置**: 只开放必要的端口
4. **端口随机化**: 考虑使用随机端口号进一步增加安全性

## 快速设置步骤

### 1. 复制配置模板
```bash
# API网关
cp ApiGateway/appsettings.template.json ApiGateway/appsettings.json

# 用户管理服务
cp UserManager/UserManager.WebAPI/appsettings.template.json UserManager/UserManager.WebAPI/appsettings.json
cp UserManager/admin_password.template.txt UserManager/admin_password.txt

# 论坛管理服务
cp ForumManager/ForumManager.WebAPI/appsettings.template.json ForumManager/ForumManager.WebAPI/appsettings.json

# 聊天服务
cp ChatService/appsettings.template.json ChatService/appsettings.json

# 匹配服务
cp MatchingService/MatchingService.WebAPI/appsettings.template.json MatchingService/MatchingService.WebAPI/appsettings.json
```

### 2. 配置数据库连接

#### PostgreSQL 数据库
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=your_db_name;Username=your_username;Password=your_password;"
  }
}
```

#### MongoDB 数据库
```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://localhost:27017"
  },
  "Mongo": {
    "Database": "your_database_name"
  }
}
```

#### Redis 缓存
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### 3. 配置JWT密钥
所有服务都需要相同的JWT配置以确保令牌验证一致性：

```json
{
  "Jwt": {
    "SecretKey": "your_super_secret_jwt_key_here",
    "Issuer": "UserManagerAPI",
    "Audience": "UserManagerClient",
    "ExpirationInMinutes": 60
  }
}
```

### 4. 配置邮件服务（仅UserManager需要）
```json
{
  "Email": {
    "SmtpHost": "smtp.your-provider.com",
    "SmtpPort": "587",
    "SenderEmail": "your-email@domain.com",
    "SenderPassword": "your_email_password",
    "SenderName": "Your App Name"
  }
}
```

## 安全建议

1. **JWT密钥**: 使用强随机字符串，建议至少32个字符
2. **数据库密码**: 使用强密码，不要使用默认密码
3. **邮件密码**: 使用应用专用密码，不要使用主账户密码
4. **管理员密码**: 定期更换，使用强密码
5. **生产环境**: 考虑使用环境变量或密钥管理服务

## 环境变量替代方案

你也可以使用环境变量来配置敏感信息，这样更安全：

```bash
# 设置环境变量
export JWT_SECRET_KEY="your_jwt_secret_key"
export DB_PASSWORD="your_db_password"
export EMAIL_PASSWORD="your_email_password"
```

然后在配置文件中使用：
```json
{
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mydb;Username=user;Password=${DB_PASSWORD};"
  }
}
```

## 故障排除

如果遇到配置问题：

1. 确保所有模板文件都已复制并重命名
2. 检查数据库连接字符串格式
3. 验证JWT密钥在所有服务中保持一致
4. 确认端口号没有冲突
5. 检查防火墙设置

## 联系支持

如果遇到配置问题，请检查：
- 数据库服务是否正在运行
- 网络连接是否正常
- 配置文件格式是否正确（JSON语法）
- 敏感信息是否正确填写
