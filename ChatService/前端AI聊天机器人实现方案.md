# 前端AI聊天机器人实现方案

## 概述

本文档描述如何在前端实现与AI聊天机器人的交互功能。聊天机器人集成阿里云百炼千问模型，支持文本对话。

## 功能特点

- ✅ 支持文本消息发送和接收
- ❌ 不支持语音消息（机器人只能处理文本）
- ❌ 不支持图片消息
- ✅ 支持上下文对话（保留最近10条消息历史）
- ✅ 自动回复用户消息

---

## 1. Token使用限制

### 1.1 限制说明

- **每个用户每天限制使用 3000 token**
- 超过限制后，机器人会提示："抱歉，您今日的AI对话次数已达上限（3000 token/天），请明天再试。"
- 使用量在每日0点（UTC时间）自动重置

### 1.2 查询使用情况

**API端点：**
```
GET /api/chatbot/usage
Authorization: Bearer {JWT_TOKEN}
```

**响应示例：**
```json
{
  "used": 1250,
  "limit": 3000,
  "remaining": 1750,
  "resetTime": "2024-11-07T00:00:00Z"
}
```

**前端实现：**
- 在聊天界面显示剩余token数量
- 接近限制时提示用户
- 达到限制时禁用发送按钮

**示例代码：**
```javascript
async function getTokenUsage() {
  const response = await fetch('/api/chatbot/usage', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const usage = await response.json();
  
  // 显示剩余额度
  document.getElementById('token-remaining').textContent = 
    `剩余额度: ${usage.remaining}/${usage.limit}`;
  
  // 如果达到限制，禁用发送按钮
  if (usage.remaining <= 0) {
    document.getElementById('send-btn').disabled = true;
    showMessage('今日AI对话次数已达上限，请明天再试');
  }
}
```

---

## 2. 获取聊天机器人信息

### 1.1 获取机器人用户ID

**API端点：**
```
GET /api/chatbot/info
Authorization: Bearer {JWT_TOKEN}
```

**响应示例：**
```json
{
  "botUserId": "ai-chatbot",
  "name": "AI助手",
  "description": "智能聊天机器人，基于阿里云百炼千问模型",
  "enabled": true,
  "supportsVoice": false,
  "supportsImage": false
}
```

**实现步骤：**
1. 应用启动时或用户登录后调用此接口
2. 获取 `botUserId`（默认为 "ai-chatbot"）
3. 确保该用户ID存在于用户的好友列表中
4. 如果不存在，需要提示用户添加该好友（或在 UserManager 中自动添加）

---

## 3. 识别聊天机器人

### 2.1 判断是否为机器人

**方法1：使用配置的机器人用户ID**
```javascript
const BOT_USER_ID = 'ai-chatbot'; // 从 /api/chatbot/info 获取

function isChatBot(userId) {
  return userId === BOT_USER_ID;
}
```

**方法2：检查消息发送者**
```javascript
function isChatBotMessage(message) {
  return message.FromUserId === BOT_USER_ID;
}
```

---

## 4. 发送消息给机器人

### 3.1 通过 SignalR 发送消息

发送方式与普通私聊消息完全相同：

```javascript
// 发送文本消息给机器人
await connection.invoke('SendPrivateMessage', 
    botUserId,      // 机器人用户ID
    '你好，请介绍一下你自己'  // 消息内容
);
```

### 3.2 注意事项

- **只支持文本消息**：机器人不支持语音、图片等消息类型
- **自动回复**：机器人会在收到消息后自动回复，无需额外操作
- **异步处理**：AI回复是异步的，可能需要几秒钟时间

---

## 5. 接收机器人回复

### 4.1 监听消息事件

机器人回复通过 `ReceivePrivateMessage` 事件接收：

```javascript
connection.on('ReceivePrivateMessage', (message) => {
  // 检查是否为机器人消息
  if (isChatBotMessage(message)) {
    console.log('收到AI回复:', message.Content);
    
    // 显示在聊天界面
    displayMessage(message);
  }
});
```

### 4.2 消息格式

机器人消息格式与普通消息相同：
```javascript
{
  Id: "message123",
  Type: "private",
  FromUserId: "ai-chatbot",  // 机器人用户ID
  ToUserId: "user123",        // 当前用户ID
  GroupId: null,
  Content: "你好！我是AI助手，很高兴为你服务。",  // AI回复内容
  CreatedAt: "2024-11-06T12:00:00Z",
  MessageType: "text"  // 机器人只支持文本消息
}
```

---

## 6. UI/UX 设计建议

### 6.1 机器人标识

**显示方式：**
- 使用特殊的头像或图标（如机器人图标）
- 添加"AI助手"标签
- 使用不同的消息气泡样式

**示例：**
```html
<div class="message bot-message">
  <div class="message-avatar bot-avatar">
    <img src="/icons/ai-bot.png" alt="AI助手">
    <span class="bot-badge">AI</span>
  </div>
  <div class="message-content">
    <div class="message-text">{message.Content}</div>
    <div class="message-time">{formatTime(message.CreatedAt)}</div>
  </div>
</div>
```

### 6.2 加载状态

**AI回复中状态：**
- 显示"AI正在思考..."提示
- 显示打字动画效果
- 禁用输入框（可选）

**实现示例：**
```javascript
async function sendMessageToBot(content) {
  // 显示发送状态
  showMessageStatus('sending');
  
  // 发送消息
  await connection.invoke('SendPrivateMessage', botUserId, content);
  
  // 显示等待AI回复状态
  showMessageStatus('waiting-for-ai');
  
  // 接收回复后会触发 ReceivePrivateMessage 事件
  // 此时隐藏等待状态
}

connection.on('ReceivePrivateMessage', (message) => {
  if (isChatBotMessage(message)) {
    hideMessageStatus('waiting-for-ai');
    displayMessage(message);
  }
});
```

### 6.3 Token使用量显示

**显示位置：**
- 聊天窗口顶部或输入框附近
- 显示格式："剩余额度: 1750/3000"
- 接近限制时显示警告（如剩余 < 500 token）

**实现示例：**
```html
<div class="token-usage-indicator">
  <span class="token-remaining">剩余额度: 1750/3000</span>
  <div class="token-progress-bar">
    <div class="token-progress" style="width: 58%"></div>
  </div>
</div>
```

```javascript
function updateTokenUsage(usage) {
  const percentage = (usage.remaining / usage.limit) * 100;
  document.querySelector('.token-progress').style.width = percentage + '%';
  document.querySelector('.token-remaining').textContent = 
    `剩余额度: ${usage.remaining}/${usage.limit}`;
  
  // 接近限制时显示警告
  if (percentage < 20) {
    document.querySelector('.token-usage-indicator').classList.add('warning');
  }
}
```

### 6.4 消息输入限制

**限制条件：**
- 只允许输入文本（禁用语音、图片等按钮）
- 限制最大长度（建议5000字符，与普通消息一致）
- 提示用户机器人只支持文本消息

**实现示例：**
```javascript
function setupChatBotInput(chatBotUserId) {
  // 禁用语音输入按钮
  document.getElementById('voice-input-btn').style.display = 'none';
  
  // 禁用图片上传按钮
  document.getElementById('image-upload-btn').style.display = 'none';
  
  // 只保留文本输入框
  const textInput = document.getElementById('text-input');
  textInput.setAttribute('maxlength', '5000');
  textInput.placeholder = '输入消息发送给AI助手（仅支持文本）';
}
```

---

## 7. 错误处理

### 7.1 AI服务不可用

**场景：**
- API Key 未配置
- 网络错误
- AI服务超时

**处理方式：**
- 机器人会回复默认错误消息："抱歉，我暂时无法回复，请稍后再试。"
- 前端可以显示提示信息
- 建议重试机制

**实现示例：**
```javascript
connection.on('ReceivePrivateMessage', (message) => {
  if (isChatBotMessage(message)) {
    // 检查是否为错误消息
    if (message.Content.includes('抱歉') || message.Content.includes('无法')) {
      showNotification('AI服务可能暂时不可用，请稍后重试', 'warning');
    }
    displayMessage(message);
  }
});
```

### 7.2 Token限制

**场景：**
- 用户今日token使用量已达3000限制

**处理方式：**
- 机器人会回复限制提示消息
- 前端可以提前检查并禁用发送按钮
- 显示重置时间倒计时

**实现示例：**
```javascript
async function sendToBot(messageText) {
  // 先检查token使用情况
  const usageResponse = await fetch('/api/chatbot/usage', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const usage = await usageResponse.json();
  
  if (usage.remaining <= 0) {
    showError('今日AI对话次数已达上限，请明天再试');
    return;
  }
  
  // 继续发送消息...
}

connection.on('ReceivePrivateMessage', (message) => {
  if (isChatBotMessage(message)) {
    // 检查是否为限制提示
    if (message.Content.includes('已达上限') || message.Content.includes('3000 token')) {
      // 禁用发送按钮
      document.getElementById('send-btn').disabled = true;
      // 显示重置时间
      showTokenLimitMessage();
    }
    displayMessage(message);
  }
});
```

### 7.3 超时处理

**实现超时检测：**
```javascript
let aiResponseTimeout = null;

function sendMessageToBot(content) {
  // 发送消息
  await connection.invoke('SendPrivateMessage', botUserId, content);
  
  // 设置超时（30秒）
  aiResponseTimeout = setTimeout(() => {
    if (aiResponseTimeout) {
      showNotification('AI回复超时，请重试', 'error');
      hideMessageStatus('waiting-for-ai');
      aiResponseTimeout = null;
    }
  }, 30000);
}

connection.on('ReceivePrivateMessage', (message) => {
  if (isChatBotMessage(message)) {
    // 清除超时
    if (aiResponseTimeout) {
      clearTimeout(aiResponseTimeout);
      aiResponseTimeout = null;
    }
    displayMessage(message);
  }
});
```

---

## 8. 上下文对话

### 8.1 工作原理

- 服务器会自动获取最近的10条对话消息作为上下文
- AI可以根据历史对话提供更准确的回复
- 前端无需特殊处理，服务器自动管理

### 8.2 上下文限制

- 最多保留最近10条消息
- 仅包含当前用户与机器人的对话
- 不会包含其他用户的对话历史

---

## 9. 完整实现流程

### 9.1 初始化

```javascript
// 1. 获取机器人信息
async function initChatBot() {
  const response = await fetch('/api/chatbot/info', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const botInfo = await response.json();
  
  // 2. 保存机器人用户ID
  window.BOT_USER_ID = botInfo.botUserId;
  
  // 3. 检查好友列表中是否包含机器人
  if (!isFriend(botInfo.botUserId)) {
    // 提示用户添加机器人好友，或在后端自动添加
    console.warn('聊天机器人未在好友列表中');
  }
  
  // 4. 设置机器人聊天界面
  if (currentChatUserId === botInfo.botUserId) {
    setupChatBotInput(botInfo.botUserId);
  }
}
```

### 9.2 发送消息

```javascript
// 发送消息给机器人
async function sendToBot(messageText) {
  if (!window.BOT_USER_ID) {
    await initChatBot();
  }
  
  // 验证消息格式
  if (!messageText || messageText.trim().length === 0) {
    return;
  }
  
  if (messageText.length > 5000) {
    showError('消息长度不能超过5000字符');
    return;
  }
  
  try {
    // 显示发送状态
    showSendingStatus();
    
    // 发送消息
    await connection.invoke('SendPrivateMessage', window.BOT_USER_ID, messageText);
    
    // 显示等待AI回复
    showWaitingForAI();
    
  } catch (error) {
    console.error('发送消息失败:', error);
    showError('发送消息失败，请重试');
    hideWaitingForAI();
  }
}
```

### 9.3 接收回复

```javascript
// 监听消息事件
connection.on('ReceivePrivateMessage', (message) => {
  // 检查是否为机器人消息
  if (message.FromUserId === window.BOT_USER_ID) {
    // 隐藏等待状态
    hideWaitingForAI();
    
    // 显示AI回复
    displayBotMessage(message);
    
    // 播放提示音（可选）
    playNotificationSound();
  } else {
    // 处理普通好友消息
    displayUserMessage(message);
  }
});
```

---

## 10. 最佳实践

### 10.1 性能优化

- **消息去重**：根据消息ID防止重复显示
- **节流控制**：限制用户发送消息的频率（建议间隔1秒）
- **缓存机器人信息**：避免频繁调用 `/api/chatbot/info`

### 10.2 用户体验

- **快速回复提示**：显示"AI正在思考..."
- **打字动画**：模拟AI打字效果（可选）
- **消息历史**：保存聊天记录，支持查看历史对话
- **快捷回复**：提供常用问题快捷按钮（如"你好"、"介绍一下自己"等）

### 10.3 安全考虑

- **内容过滤**：后端会过滤敏感内容（由AI模型处理）
- **频率限制**：防止恶意刷屏（可在后端实现）
- **消息验证**：确保只发送文本消息

---

## 11. 示例代码

### 11.1 完整的聊天机器人组件（React示例）

```javascript
import React, { useState, useEffect } from 'react';

function ChatBotComponent({ connection, token }) {
  const [botUserId, setBotUserId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [inputText, setInputText] = useState('');
  const [isWaitingForAI, setIsWaitingForAI] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // 初始化机器人
  useEffect(() => {
    async function initBot() {
      try {
        const response = await fetch('/api/chatbot/info', {
          headers: { 'Authorization': `Bearer ${token}` }
        });
        const botInfo = await response.json();
        setBotUserId(botInfo.botUserId);
        setIsLoading(false);
      } catch (error) {
        console.error('获取机器人信息失败:', error);
      }
    }
    
    initBot();
    
    // 监听消息
    connection.on('ReceivePrivateMessage', handleMessage);
    connection.on('PrivateMessageSent', handleMessageSent);
    
    return () => {
      connection.off('ReceivePrivateMessage', handleMessage);
      connection.off('PrivateMessageSent', handleMessageSent);
    };
  }, [connection, token]);

  const handleMessage = (message) => {
    if (message.FromUserId === botUserId || message.ToUserId === botUserId) {
      setMessages(prev => [...prev, message]);
      if (message.FromUserId === botUserId) {
        setIsWaitingForAI(false);
      }
    }
  };

  const handleMessageSent = (message) => {
    if (message.ToUserId === botUserId) {
      setMessages(prev => [...prev, message]);
      setIsWaitingForAI(true);
    }
  };

  const sendMessage = async () => {
    if (!inputText.trim() || !botUserId) return;

    try {
      await connection.invoke('SendPrivateMessage', botUserId, inputText.trim());
      setInputText('');
    } catch (error) {
      console.error('发送消息失败:', error);
      alert('发送消息失败，请重试');
    }
  };

  if (isLoading) {
    return <div>加载中...</div>;
  }

  return (
    <div className="chat-bot-container">
      <div className="chat-header">
        <h3>AI助手</h3>
        <span className="bot-badge">AI</span>
      </div>
      
      <div className="messages-container">
        {messages.map(msg => (
          <div key={msg.Id} className={`message ${msg.FromUserId === botUserId ? 'bot-message' : 'user-message'}`}>
            <div className="message-content">{msg.Content}</div>
            <div className="message-time">{new Date(msg.CreatedAt).toLocaleTimeString()}</div>
          </div>
        ))}
        
        {isWaitingForAI && (
          <div className="message bot-message">
            <div className="message-content">
              <span className="typing-indicator">AI正在思考...</span>
            </div>
          </div>
        )}
      </div>
      
      <div className="input-container">
        <input
          type="text"
          value={inputText}
          onChange={(e) => setInputText(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
          placeholder="输入消息（仅支持文本）"
          maxLength={5000}
        />
        <button onClick={sendMessage} disabled={!inputText.trim()}>
          发送
        </button>
      </div>
    </div>
  );
}

export default ChatBotComponent;
```

---

## 12. 配置要求

### 12.1 后端配置

确保 `appsettings.json` 中配置了阿里云API Key：

```json
{
  "AiChatBot": {
    "BotUserId": "ai-chatbot",
    "Aliyun": {
      "ApiKey": "your-api-key-here",
      "Model": "qwen-turbo"
    }
  }
}
```

### 12.2 用户管理

需要在 UserManager 服务中确保存在用户ID为 `ai-chatbot` 的用户，或者将配置的 `BotUserId` 对应的用户添加到所有用户的好友列表中。

---

## 13. 总结

前端实现AI聊天机器人主要包括：
1. **初始化**：获取机器人用户ID和配置
2. **发送消息**：通过 SignalR 发送文本消息给机器人
3. **接收回复**：监听 `ReceivePrivateMessage` 事件接收AI回复
4. **UI展示**：区分机器人消息和用户消息，显示加载状态
5. **错误处理**：处理超时、服务不可用等情况

按照以上方案实现，即可完成AI聊天机器人功能的前端集成。

