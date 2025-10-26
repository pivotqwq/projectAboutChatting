# UserManager API 接口文档

## 基础信息

- **Base URL**: `/api`
- **数据格式**: JSON
- **编码**: UTF-8

---

## 目录

1. [登录相关接口 (Login)](#登录相关接口)
2. [用户管理接口 (UsersMgr)](#用户管理接口)

---

## 登录相关接口

### 1. 账号密码登录（已测通）

**接口地址**: `POST /api/Login/LoginByPhoneAndPassword`

**接口描述**: 使用手机号/邮箱+密码进行登录

**请求参数**:

```json
{
  "userBasic": {
    "phoneNumber": "string",  // 手机号（可选，与邮箱至少提供一个）
    "email": "string"         // 邮箱（可选，与手机号至少提供一个）
  },
  "password": "string"        // 密码（长度必须大于6）
}
```

**请求示例**:

```json
{
  "userBasic": {
    "phoneNumber": "13800138000",
    "email": "user@example.com"
  },
  "password": "password123"
}
```

**成功响应** (200 OK):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "用户名"
}
```

**错误响应**:

- `400 Bad Request`: "密码长度必须大于6"
- `400 Bad Request`: "请至少输入手机号或邮箱中的一个"
- `400 Bad Request`: "账号或者密码错误"
- `400 Bad Request`: "用户被锁定，请稍后再试"

---

### 2. 发送验证码（手机短信）（不可用）

**接口地址**: `POST /api/Login/SendVerificationCode`

**接口描述**: 向用户手机或邮箱发送验证码

**请求参数**:

```json
{
  "userBasic": {
    "phoneNumber": "string",
    "email": "string"
  }
}
```

**请求示例**:

```json
{
  "userBasic": {
    "phoneNumber": "13800138000",
    "email": null
  }
}
```

**成功响应** (200 OK):

```json
"验证码已发送"
```

**错误响应**:

- `400 Bad Request`: "请至少输入手机号或邮箱中的一个"
- `400 Bad Request`: "用户不存在"
- `400 Bad Request`: "用户被锁定，请稍后再试"

---

### 3. 手机验证码登录（不可用）

**接口地址**: `POST /api/Login/LoginByPhoneAndCode`

**接口描述**: 使用手机号/邮箱+验证码进行登录

**请求参数**:

```json
{
  "userBasic": {
    "phoneNumber": "string",  // 手机号
    "email": "string"         // 邮箱
  },
  "code": "string"            // 验证码
}
```

**请求示例**:

```json
{
  "userBasic": {
    "phoneNumber": "13800138000",
    "email": null
  },
  "code": "123456"
}
```

**成功响应** (200 OK):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "用户名"
}
```

**错误响应**:

- `400 Bad Request`: "请至少输入手机号或邮箱中的一个"
- `400 Bad Request`: "验证码不能为空"
- `400 Bad Request`: "用户不存在"
- `400 Bad Request`: "用户被锁定，请稍后再试"
- `400 Bad Request`: "验证码错误或已过期"

---

### 4. 发送邮箱验证码（已测通）

**接口地址**: `POST /api/Login/SendEmailCode`

**接口描述**: 向用户邮箱发送验证码

**请求参数**:

```json
{
  "email": "string"  // 邮箱地址（必填）
}
```

**请求示例**:

```json
{
  "email": "user@example.com"
}
```

**成功响应** (200 OK):

```json
"验证码已发送到您的邮箱"
```

**错误响应**:

- `400 Bad Request`: "邮箱不能为空"
- `400 Bad Request`: "用户不存在"
- `400 Bad Request`: "用户被锁定，请稍后再试"

---

### 5. 邮箱验证码登录（已测通）

**接口地址**: `POST /api/Login/LoginByEmailCode`

**接口描述**: 使用邮箱+验证码进行登录

**请求参数**:

```json
{
  "email": "string",  // 邮箱地址（必填）
  "code": "string"    // 验证码（必填）
}
```

**请求示例**:

```json
{
  "email": "user@example.com",
  "code": "123456"
}
```

**成功响应** (200 OK):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "用户名"
}
```

**错误响应**:

- `400 Bad Request`: "邮箱不能为空"
- `400 Bad Request`: "验证码不能为空"
- `400 Bad Request`: "用户不存在"
- `400 Bad Request`: "用户被锁定，请稍后再试"
- `400 Bad Request`: "验证码错误或已过期"

---

### 6. 发送注册邮箱验证码（已测通）

**接口地址**: `POST /api/Login/SendRegisterEmailCode`

**接口描述**: 向待注册的邮箱发送验证码，用于注册验证

**请求参数**:

```json
{
  "email": "string"  // 邮箱地址（必填）
}
```

**请求示例**:

```json
{
  "email": "newuser@example.com"
}
```

**成功响应** (200 OK):

```json
"验证码已发送到您的邮箱"
```

**错误响应**:

- `400 Bad Request`: "邮箱不能为空"
- `400 Bad Request`: "该邮箱已被注册"
- `500 Internal Server Error`: "邮箱验证码服务未配置"

---

## 用户管理接口

### 1. 用户注册（已测通）

**接口地址**: `POST /api/UsersMgr/AddNew`

**接口描述**: 注册新用户（需要先调用发送注册邮箱验证码接口获取验证码）

**请求参数**:

```json
{
  "userBasic": {
    "phoneNumber": "string",  // 手机号（必填）
    "email": "string"         // 邮箱（必填）
  },
  "password": "string",       // 密码（必填）
  "emailCode": "string"       // 邮箱验证码（必填）
}
```

**请求示例**:

```json
{
  "userBasic": {
    "phoneNumber": "13800138000",
    "email": "newuser@example.com"
  },
  "password": "password123",
  "emailCode": "123456"
}
```

**成功响应** (200 OK):

```json
"注册成功"
```

**错误响应**:

- `400 Bad Request`: "请输入手机号"
- `400 Bad Request`: "请输入邮箱"
- `400 Bad Request`: "请输入邮箱验证码"
- `400 Bad Request`: "邮箱验证码错误或已过期"
- `400 Bad Request`: "该手机号或邮箱已经被注册"

**注册流程**:

1. 调用 `POST /api/Login/SendRegisterEmailCode` 发送验证码到邮箱
2. 用户收到验证码后，调用此接口完成注册

---

### 2. 修改密码（管理员）(已测通)

**接口地址**: `PUT /api/UsersMgr/AdminChangePassword`

**接口描述**: 管理员修改指定用户的密码（需要管理员密码）

**认证要求**: 需要在请求头中提供管理员密码

**请求头**:

```
X-Admin-Password: {admin_password}
```

**请求参数**:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",  // 用户ID（必填）
  "password": "string"                            // 新密码（必填）
}
```

**请求示例**:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "password": "newpassword123"
}
```

**成功响应** (200 OK):

```json
"成功"
```

**错误响应**:

- `404 Not Found`: 用户不存在

---

### 3. 解锁用户（管理员）（已测通）

**接口地址**: `PUT /api/UsersMgr/Unlock/{id}`

**接口描述**: 解锁被锁定的用户账号（需要管理员密码）

**认证要求**: 需要在请求头中提供管理员密码

**请求头**:

```
X-Admin-Password: {admin_password}
```

**请求参数**: 

- 路径参数: `id` (Guid) - 用户ID

**请求示例**:

```
PUT /api/UsersMgr/Unlock/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**成功响应** (200 OK):

```json
"成功"
```

**错误响应**:

- `404 Not Found`: 用户不存在

---

### 4. 获取所有用户（已测通）

**接口地址**: `GET /api/UsersMgr/GetAll`

**接口描述**: 获取所有用户列表

**认证要求**: 需要JWT Token

**请求头**:

```
Authorization: Bearer {access_token}
```

**成功响应** (200 OK):

```json
[
  "用户名1",
  "用户名2",
  "用户名3"
]
```

**错误响应**:

- `401 Unauthorized`: 未授权（Token无效或未提供）

---

### 5. 获取当前登录用户信息（已测通）

**接口地址**: `GET /api/UsersMgr/GetCurrentUser`

**接口描述**: 获取当前登录用户的用户名

**认证要求**: 需要JWT Token

**请求头**:

```
Authorization: Bearer {access_token}
```

**成功响应** (200 OK):

```json
"用户名"
```

**错误响应**:

- `401 Unauthorized`: "无效的Token"
- `404 Not Found`: "用户不存在"

---

### 6. 通过邮箱验证码修改密码（已测通）

**接口地址**: `PUT /api/UsersMgr/ChangePasswordByEmail`

**接口描述**: 通过邮箱验证码修改密码（无需认证，需要先调用发送邮箱验证码接口）

**认证要求**: 无需认证

**请求参数**:

```json
{
  "email": "string",      // 邮箱地址（必填）
  "emailCode": "string",  // 邮箱验证码（必填）
  "newPassword": "string" // 新密码（必填）
}
```

**请求示例**:

```json
{
  "email": "user@example.com",
  "emailCode": "123456",
  "newPassword": "newpassword456"
}
```

**成功响应** (200 OK):

```json
"密码修改成功"
```

**错误响应**:

- `400 Bad Request`: "请输入邮箱地址"
- `400 Bad Request`: "请输入邮箱验证码"
- `400 Bad Request`: "请输入新密码"
- `400 Bad Request`: "用户不存在"
- `400 Bad Request`: "用户被锁定，请稍后再试"
- `400 Bad Request`: "验证码错误或已过期"
- `404 Not Found`: "用户不存在"

---

### 4. 锁定用户（管理员）（已测通）

**接口地址**: `PUT /api/UsersMgr/LockUser`

**接口描述**: 锁定指定用户（需要管理员密码）

**认证要求**: 需要在请求头中提供管理员密码

**请求头**:

```
X-Admin-Password: {admin_password}
```

**请求参数**:

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",  // 用户ID（必填）
  "lockMinutes": 60,                                   // 锁定时长（分钟），0表示永久锁定（必填）
  "reason": "string"                                   // 锁定原因（可选）
}
```

**请求示例**:

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "lockMinutes": 60,
  "reason": "违规行为"
}
```

**成功响应** (200 OK):

```json
"用户已锁定 60 分钟"
```

**错误响应**:

- `401 Unauthorized`: "缺少管理员密码"
- `403 Forbidden`: "管理员密码错误"
- `404 Not Found`: "用户不存在"

---

### 5. 获取当前用户个人信息（已测通）

**接口地址**: `GET /api/UsersMgr/GetMyProfile`

**接口描述**: 获取当前登录用户的个人信息

**认证要求**: 需要JWT Token

**请求头**:

```
Authorization: Bearer {access_token}
```

**成功响应** (200 OK):

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "avatar": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
  "realName": "张三",
  "gender": "Male",
  "birthday": "1990-01-01T00:00:00Z",
  "region": "北京市",
  "phoneNumber": "13800138000",
  "bio": "这是我的个人简介",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

**错误响应**:

- `401 Unauthorized`: "无效的Token"
- `404 Not Found`: "个人信息不存在"

---

### 6. 更新当前用户个人信息（已测通）

**接口地址**: `PUT /api/UsersMgr/UpdateMyProfile`

**接口描述**: 更新当前登录用户的个人信息

**认证要求**: 需要JWT Token

**请求头**:

```
Authorization: Bearer {access_token}
```

**请求参数**:

```json
{
  "avatar": "string",        // 头像Base64编码（可选）
  "realName": "string",      // 真实姓名（可选）
  "gender": "string",        // 性别：Male/Female/Other（可选）
  "birthday": "2024-01-01T00:00:00Z", // 生日（可选）
  "region": "string",        // 地区（可选）
  "phoneNumber": "string",   // 手机号（可选）
  "bio": "string"           // 个人简介（可选）
}
```

**请求示例**:

```json
{
  "avatar": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
  "realName": "张三",
  "gender": "Male",
  "birthday": "1990-01-01T00:00:00Z",
  "region": "北京市",
  "phoneNumber": "13800138000",
  "bio": "这是我的个人简介"
}
```

**成功响应** (200 OK):

```json
"个人信息更新成功"
```

**错误响应**:

- `401 Unauthorized`: "无效的Token"
- `404 Not Found`: "个人信息不存在"

---

## 通用说明

### 认证方式

部分接口需要JWT认证，需要在请求头中携带Token：

```
Authorization: Bearer {access_token}
```

### 响应状态码

- `200 OK`: 请求成功
- `400 Bad Request`: 请求参数错误或业务逻辑错误
- `401 Unauthorized`: 未授权或Token无效
- `404 Not Found`: 资源不存在
- `500 Internal Server Error`: 服务器内部错误

### UserBasic 对象说明

`UserBasic` 是用户基本信息的值对象，包含：

- `phoneNumber`: 手机号（可选）
- `email`: 邮箱（可选）

在大多数接口中，手机号和邮箱至少需要提供一个。

### LoginResponse 对象说明

登录成功后返回的对象包含：

- `accessToken`: 访问令牌，用于后续API调用
- `refreshToken`: 刷新令牌（暂未实现刷新逻辑）
- `tokenType`: 令牌类型，固定为 "Bearer"
- `expiresIn`: Token过期时间（秒），默认3600秒（60分钟）
- `userId`: 用户唯一标识
- `userName`: 用户名称

### 用户锁定机制

系统具有用户锁定机制，多次登录失败会导致账号被锁定。锁定后需要管理员调用解锁接口进行解锁。

 

