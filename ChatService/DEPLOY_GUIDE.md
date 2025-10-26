# ChatService 部署指南

## ✅ 发布已完成

发布目录：`D:\ForumAndChatRoomProject\ChatService\publish`

---

## 📤 上传到服务器

### 方法1：使用FTP/SFTP工具

1. 使用FileZilla、WinSCP等工具
2. 连接到服务器 `47.99.79.0`
3. 导航到 `/www/wwwroot/backend/chatService/chat_publish`
4. 上传 `publish` 目录的**所有内容**（不是publish文件夹本身）
5. 覆盖原有文件

### 方法2：使用SCP命令

```powershell
# 在PowerShell中执行
scp -r .\publish\* root@47.99.79.0:/www/wwwroot/backend/chatService/chat_publish/
```

### 方法3：使用rsync（如果安装了）

```bash
rsync -avz ./publish/ root@47.99.79.0:/www/wwwroot/backend/chatService/chat_publish/
```

---

## 🔄 重启服务器上的应用

上传完成后，在Linux服务器上执行：

```bash
# 查找ChatService进程
ps aux | grep ChatService
# 或
ps aux | grep dotnet | grep chat

# 如果找到进程ID（例如12345），杀掉进程
sudo kill -9 进程ID

# 进入应用目录
cd /www/wwwroot/backend/chatService/chat_publish

# 启动应用
nohup dotnet ChatService.dll > app.log 2>&1 &

# 查看日志确认启动
tail -f app.log
```

**或者**如果使用systemd/supervisor：

```bash
# systemd
sudo systemctl restart chatservice

# supervisor
sudo supervisorctl restart chatservice
```

---

## ✅ 验证部署

### 1. 检查应用是否运行

```bash
# 检查端口9293是否在监听
sudo netstat -tulpn | grep 9293
```

### 2. 测试API

```bash
curl http://localhost:9293/api/presence/online-users
```

### 3. 查看日志

```bash
# 如果使用nohup
tail -f /www/wwwroot/backend/chatService/chat_publish/app.log

# 如果使用systemd
sudo journalctl -u chatservice -f
```

---

## 🎯 本次更新内容

- ✅ 添加CORS支持（允许前端跨域访问）
- ✅ 配置SignalR允许跨域WebSocket连接
- ✅ Redis连接配置优化

---

## ⚠️ 注意事项

1. **CORS配置**：现在允许所有来源访问，生产环境建议限制特定域名
2. **Redis配置**：确保Redis正在运行（`redis-cli ping`应返回PONG）
3. **MongoDB配置**：确保MongoDB正在运行

---

部署完成后，前端就可以正常连接SignalR了！

