# 🚀 START HERE - 开始使用聊天测试

## ⚡ 三步开始

### 1️⃣ 登录
```
输入手机号/邮箱 → 输入密码 → 点击"登录"
```

### 2️⃣ 连接（重要！）
```
点击顶部绿色的 "⚡ 立即连接" 按钮
等待状态变为 "✅ 在线"
```

**⚠️ 必须先连接才能发消息！按钮被禁用是因为未连接！**

### 3️⃣ 开始聊天
```
点击左侧 + → 输入对方用户ID → 发送消息
```

---

## 📋 查看/关闭调试日志

**打开日志**：点击顶部橙色 `📋 日志` 按钮  
**关闭日志**：再次点击变成 `🔽 关闭`，或点击日志面板右上角 `×`

---

## ❓ 常见问题

**Q: 按钮都是灰色，点不了？**  
A: 正常！必须先点击顶部"⚡ 立即连接"，连接成功后才能用

**Q: 消息发不出去？**  
A: 检查顶部状态是否为"✅ 在线"

**Q: 显示401错误？**  
A: 需要更新服务器ChatService，请看下面部署说明

---

## 📦 部署最新ChatService（如果有401/500错误）

### 方式1：使用FTP工具
1. 打开WinSCP/FileZilla
2. 连接：47.99.79.0
3. 上传：`D:\ForumAndChatRoomProject\ChatService\publish\*`
4. 目标：`/www/wwwroot/backend/chatService/chat_publish/`
5. 覆盖所有文件

### 方式2：使用命令行（需要SSH工具）
```powershell
# Windows PowerShell
scp -r D:\ForumAndChatRoomProject\ChatService\publish\* root@47.99.79.0:/www/wwwroot/backend/chatService/chat_publish/
```

### 重启ChatService
```bash
# SSH连接服务器
ssh root@47.99.79.0

# 重启服务
pkill -f ChatService.dll
cd /www/wwwroot/backend/chatService/chat_publish
nohup dotnet ChatService.dll > app.log 2>&1 &

# 查看日志
tail -f app.log
```

---

## 🧪 多用户测试

1. **标签页A（正常模式）**：登录用户A，点击连接
2. **标签页B（无痕 Ctrl+Shift+N）**：登录用户B，点击连接
3. **A给B发消息** → B实时收到
4. **B回复** → A实时收到

---

## ✅ 修复了什么

最新版本修复了：
- ✅ JWT Token验证（401错误）
- ✅ MongoDB数据格式兼容（500错误）
- ✅ 消息字段大小写兼容（PascalCase/camelCase）
- ✅ SignalR连接稳定性
- ✅ 界面提示更清晰
- ✅ 支持离线消息

---

**现在刷新页面，按照步骤操作就能正常使用了！** 🎉

