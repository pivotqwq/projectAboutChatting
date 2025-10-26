<template>
  <div class="chat-app">
    <!-- 登录界面 -->
    <div v-if="!token" class="login-container">
      <div class="login-box">
        <h2>💬 ChatService 测试</h2>
        
        <div class="tips-box">
          <div style="font-size: 13px; color: #666; line-height: 1.6;">
            💡 <strong>测试提示：</strong><br>
            • 使用已在UserManager注册的账号登录<br>
            • 至少输入手机号或邮箱其中一个<br>
            • 密码长度必须大于6位<br>
            • 可以打开多个标签页测试多用户聊天
          </div>
        </div>

        <div class="form-group">
          <label>UserManager地址</label>
          <input v-model="userManagerUrl" placeholder="http://47.99.79.0:9291" class="input" />
        </div>
        <div class="form-group">
          <label>ChatService地址</label>
          <input v-model="chatServiceUrl" placeholder="http://47.99.79.0:9293" class="input" />
        </div>
        <div class="form-group">
          <label>手机号 *</label>
          <input v-model="phoneNumber" placeholder="输入手机号（或邮箱）" class="input" />
        </div>
        <div class="form-group">
          <label>邮箱（可选）</label>
          <input v-model="email" placeholder="邮箱（可选）" class="input" />
        </div>
        <div class="form-group">
          <label>密码 *</label>
          <input v-model="password" type="password" placeholder="密码（长度>6）" class="input" @keyup.enter="login" />
        </div>
        <button @click="login" class="btn-login">登录</button>
        <div v-if="loginError" class="error-message">
          ❌ {{ loginError }}
        </div>
      </div>
    </div>

    <!-- 聊天主界面 -->
    <div v-else class="chat-main">
      <!-- 警告横幅 -->
      <div v-if="showCorsWarning" class="warning-banner">
        ⚠️ <strong>提示：</strong>如果SignalR无法连接或API返回401错误，说明服务器上的ChatService需要更新CORS配置。
        请参考 <code>ChatService/DEPLOY_GUIDE.md</code> 重新部署。
        <button @click="showCorsWarning = false" class="btn-close-banner">×</button>
      </div>

      <!-- 顶部栏 -->
      <div class="top-bar">
        <div class="user-info">
          <span class="user-avatar">{{ userName.substring(0, 2) }}</span>
          <div class="user-details">
            <div class="user-name">{{ userName }}</div>
            <div class="user-status">
              <span :class="isConnected ? 'status-dot online' : 'status-dot offline'"></span>
              <strong :style="{ color: isConnected ? '#4caf50' : '#f44336' }">
                {{ isConnected ? '✅ 在线' : '⚠️ 离线' }}
              </strong>
            </div>
          </div>
        </div>
        <div class="top-actions">
          <button 
            @click="connectSignalR" 
            v-if="!isConnected" 
            class="btn-connect-big"
          >
            ⚡ 立即连接
          </button>
          <button @click="disconnectSignalR" v-else class="btn-sm btn-danger">断开连接</button>
          <button @click="showDebugLog = !showDebugLog" class="btn-sm btn-log">
            {{ showDebugLog ? '🔽' : '📋' }} {{ showDebugLog ? '关闭' : '日志' }}
          </button>
          <button @click="logout" class="btn-sm">退出</button>
        </div>
      </div>

      <!-- 主体内容 -->
      <div class="main-container">
        <!-- 左侧：联系人列表 -->
        <div class="sidebar">
          <div class="sidebar-header">
            <h3>联系人</h3>
            <button @click="getOnlineUsers" class="btn-icon" title="刷新在线用户">🔄</button>
          </div>
          
          <!-- 连接提示 -->
          <div v-if="!isConnected" class="sidebar-tip warning">
            ⚠️ 未连接ChatHub<br>
            <small>请先点击顶部"连接"按钮</small>
          </div>

          <!-- 联系人列表 -->
          <div class="contact-list">
            <div class="list-section-title">
              联系人 ({{ allContacts.length }})
              <button @click="showNewChatDialog = true" class="btn-icon-sm" title="新建聊天">+</button>
            </div>
            
            <!-- 所有联系人（在线+离线+聊天记录） -->
            <div 
              v-for="user in allContacts" 
              :key="'user-' + user"
              :class="['contact-item', { active: currentChatType === 'private' && currentChatId === user }]"
              @click="openPrivateChat(user)"
            >
              <div class="contact-avatar">{{ user.substring(0, 2) }}</div>
              <div class="contact-info">
                <div class="contact-name">{{ user }}</div>
                <div class="contact-status">
                  <span v-if="isUserOnline(user)" class="online-dot">●</span>
                  {{ isUserOnline(user) ? '在线' : '离线' }}
                </div>
              </div>
              <span v-if="getUnreadCount('private', user) > 0" class="unread-badge">
                {{ getUnreadCount('private', user) }}
              </span>
            </div>
            
            <div v-if="allContacts.length === 0" class="empty-tip">
              暂无联系人<br>
              点击 + 开始新聊天
            </div>
          </div>

          <!-- 群组列表 -->
          <div class="contact-list">
            <div class="list-section-title">群组 ({{ joinedGroups.length }})</div>
            <div 
              v-for="group in joinedGroups" 
              :key="'group-' + group"
              :class="['contact-item', { active: currentChatType === 'group' && currentChatId === group }]"
              @click="openGroupChat(group)"
            >
              <div class="contact-avatar group">👥</div>
              <div class="contact-info">
                <div class="contact-name">{{ group }}</div>
                <div class="contact-status">群聊</div>
              </div>
              <span v-if="getUnreadCount('group', group) > 0" class="unread-badge">
                {{ getUnreadCount('group', group) }}
              </span>
            </div>
            <div class="contact-item add-btn" @click="showJoinGroupDialog = true">
              <div class="contact-avatar">+</div>
              <div class="contact-info">
                <div class="contact-name">加入群组</div>
              </div>
            </div>
          </div>
        </div>

        <!-- 右侧：聊天窗口 -->
        <div class="chat-panel">
          <!-- 聊天头部 -->
          <div class="chat-header">
            <div v-if="currentChatType === 'private' && currentChatId">
              <div class="chat-title">{{ currentChatId }}</div>
              <div class="chat-subtitle">私聊</div>
            </div>
            <div v-else-if="currentChatType === 'group' && currentChatId">
              <div class="chat-title">{{ currentChatId }}</div>
              <div class="chat-subtitle">群聊</div>
            </div>
            <div v-else class="chat-title-empty">
              请选择联系人开始聊天
            </div>
            
            <div class="chat-actions" v-if="currentChatType === 'group' && currentChatId">
              <button @click="leaveCurrentGroup" class="btn-sm btn-danger">退出群组</button>
            </div>
          </div>

          <!-- 消息区域 -->
          <div class="messages-container" ref="messagesContainer">
            <div v-if="!currentChatId" class="welcome-screen">
              <div class="welcome-icon">💬</div>
              <h2>欢迎使用聊天测试</h2>
              
              <!-- 连接状态提示 -->
              <div v-if="!isConnected" class="connection-warning">
                <div class="warning-icon">⚠️</div>
                <h3>未连接到ChatHub</h3>
                <p>请先点击顶部的<strong>"连接"</strong>按钮</p>
                <button @click="connectSignalR" class="btn-primary-big">立即连接</button>
              </div>
              
              <div v-else>
                <p style="color: #4caf50; font-size: 16px;">✅ 已连接 - 可以开始聊天了</p>
                <p>从左侧选择联系人或点击 <strong>+</strong> 新建聊天</p>
                <div class="welcome-tips">
                  <div>💡 使用提示：</div>
                  <div>• 点击左侧联系人列表的 <strong>+</strong> 按钮开始新聊天</div>
                  <div>• 打开多个浏览器标签页用不同账号登录测试</div>
                  <div>• 支持离线消息（对方离线也能发送）</div>
                  <div>• 所有人加入同一个群组可以测试群聊</div>
                </div>
              </div>
            </div>

            <div v-else class="messages-list" :key="'messages-' + refreshKey">
              <div 
                v-for="(msg, index) in getCurrentMessages" 
                :key="msg.id || msg.Id || `msg-${index}-${refreshKey}`"
                :class="['message-wrapper', msg.fromUserId === userId || msg.FromUserId === userId ? 'mine' : 'theirs']"
              >
                <div class="message-avatar">
                  {{ (msg.fromUserId || msg.FromUserId || 'U')?.substring(0, 2) }}
                </div>
                <div class="message-box">
                  <div class="message-sender" v-if="(msg.fromUserId || msg.FromUserId) !== userId">
                    {{ msg.fromUserId || msg.FromUserId || '未知用户' }}
                  </div>
                  <div class="message-bubble">{{ msg.content || msg.Content || '(空消息)' }}</div>
                  <div class="message-time">{{ formatTime(msg.createdAt || msg.CreatedAt) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- 输入区域 -->
          <div class="input-container">
            <input 
              v-model="messageInput"
              @keyup.enter="sendCurrentMessage"
              :disabled="!isConnected || !currentChatId"
              :placeholder="!isConnected ? '请先连接ChatHub...' : !currentChatId ? '请先选择联系人...' : '输入消息（对方离线也会收到）...'"
              class="message-input"
            />
            <button 
              @click="sendCurrentMessage"
              :disabled="!isConnected || !currentChatId || !messageInput.trim()"
              class="btn-send"
            >
              发送
            </button>
          </div>
          <div v-if="currentChatType === 'private' && currentChatId && !isUserOnline(currentChatId)" 
               class="offline-tip">
            💡 对方当前离线，消息会存储在服务器，对方上线后会看到
          </div>
        </div>
      </div>
    </div>

    <!-- 新建聊天对话框 -->
    <div v-if="showNewChatDialog" class="modal-overlay" @click="showNewChatDialog = false">
      <div class="modal-box" @click.stop>
        <h3>新建聊天</h3>
        <p style="font-size: 13px; color: #666; margin-bottom: 15px;">
          输入对方的用户ID开始聊天（对方可以离线）
        </p>
        <input v-model="newChatUserId" placeholder="输入用户ID" class="input" @keyup.enter="startNewChat" />
        <div class="modal-actions">
          <button @click="showNewChatDialog = false" class="btn-cancel">取消</button>
          <button @click="startNewChat" class="btn-primary">开始聊天</button>
        </div>
      </div>
    </div>

    <!-- 加入群组对话框 -->
    <div v-if="showJoinGroupDialog" class="modal-overlay" @click="showJoinGroupDialog = false">
      <div class="modal-box" @click.stop>
        <h3>加入群组</h3>
        <input v-model="newGroupId" placeholder="输入群组ID（如：group001）" class="input" @keyup.enter="joinNewGroup" />
        <div class="modal-actions">
          <button @click="showJoinGroupDialog = false" class="btn-cancel">取消</button>
          <button @click="joinNewGroup" class="btn-primary">加入</button>
        </div>
      </div>
    </div>

    <!-- 调试日志面板 -->
    <div v-if="showDebugLog" class="debug-panel">
      <div class="debug-header">
        <span>📋 调试日志</span>
        <button @click="clearLogs" class="btn-sm">清空</button>
        <button @click="showDebugLog = false" class="btn-close">×</button>
      </div>
      <div class="debug-content">
        <div v-for="(log, index) in logs" :key="index" class="log-item">
          <span class="log-time">{{ log.time }}</span>
          <span :class="'log-' + log.type">{{ log.message }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, nextTick } from 'vue'
import * as signalR from '@microsoft/signalr'
import axios from 'axios'

// 配置
const userManagerUrl = ref('http://47.99.79.0:9291')
const chatServiceUrl = ref('http://47.99.79.0:9293')

// 登录信息
const phoneNumber = ref('')
const email = ref('')
const password = ref('')
const token = ref('')
const userId = ref('')
const userName = ref('')
const loginError = ref('')

// SignalR连接
let connection = null
const isConnected = ref(false)

// 当前聊天
const currentChatType = ref('') // 'private' or 'group'
const currentChatId = ref('') // userId or groupId

// 消息存储 { 'private:userId': [], 'group:groupId': [] }
const messages = ref({})
const messageInput = ref('')
const refreshKey = ref(0) // 用于强制刷新界面

// 联系人和群组
const onlineUsers = ref([])
const chattedUsers = ref([]) // 聊过天的用户（包括离线）
const joinedGroups = ref([])
const newGroupId = ref('group001')
const newChatUserId = ref('')
const showJoinGroupDialog = ref(false)
const showNewChatDialog = ref(false)

// 未读消息计数
const unreadCount = ref({})

// 调试
const logs = ref([])
const showDebugLog = ref(false)
const messagesContainer = ref(null)

// UI状态
const showCorsWarning = ref(true)

// 计算属性：所有联系人（在线用户 + 聊过天的用户，去重）
const allContacts = computed(() => {
  const contacts = new Set()
  
  // 添加在线用户（排除自己）
  onlineUsers.value.forEach(u => {
    if (u !== userId.value) contacts.add(u)
  })
  
  // 添加聊过天的用户
  chattedUsers.value.forEach(u => {
    if (u !== userId.value) contacts.add(u)
  })
  
  // 添加有消息记录的用户
  Object.keys(messages.value).forEach(key => {
    if (key.startsWith('private:')) {
      const targetUser = key.substring(8) // 'private:'.length = 8
      if (targetUser !== userId.value) contacts.add(targetUser)
    }
  })
  
  return Array.from(contacts).sort()
})

// 判断用户是否在线
function isUserOnline(targetUserId) {
  return onlineUsers.value.includes(targetUserId)
}

// 获取当前聊天的消息（使用computed自动响应变化）
const getCurrentMessages = computed(() => {
  if (!currentChatId.value) return []
  const key = `${currentChatType.value}:${currentChatId.value}`
  return messages.value[key] || []
})

// 获取未读消息数
function getUnreadCount(type, id) {
  const key = `${type}:${id}`
  return unreadCount.value[key] || 0
}

// 清除未读消息
function clearUnread(type, id) {
  const key = `${type}:${id}`
  // 触发响应式更新
  unreadCount.value = {
    ...unreadCount.value,
    [key]: 0
  }
}

// 添加日志
function addLog(message, type = 'info') {
  const time = new Date().toLocaleTimeString('zh-CN')
  logs.value.unshift({ time, message, type })
  if (logs.value.length > 200) logs.value.pop()
}

function clearLogs() {
  logs.value = []
}

// 格式化时间
function formatTime(dateStr) {
  if (!dateStr) return ''
  
  const date = new Date(dateStr)
  if (isNaN(date.getTime())) return ''
  
  const now = new Date()
  const diff = now - date
  
  if (diff < 60000) return '刚刚'
  if (diff < 3600000) return `${Math.floor(diff / 60000)}分钟前`
  if (diff < 86400000) return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  return date.toLocaleString('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

// 滚动到底部
async function scrollToBottom() {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

// 标准化消息对象（兼容不同的字段名格式）
function normalizeMessage(msg) {
  return {
    id: msg.id || msg.Id || msg._id || Math.random().toString(),
    type: msg.type || msg.Type || 'private',
    fromUserId: msg.fromUserId || msg.FromUserId || 'unknown',
    toUserId: msg.toUserId || msg.ToUserId || null,
    groupId: msg.groupId || msg.GroupId || null,
    content: msg.content || msg.Content || '',
    createdAt: msg.createdAt || msg.CreatedAt || new Date().toISOString()
  }
}

// 添加消息到列表
function addMessageToList(type, id, message) {
  const key = `${type}:${id}`
  
  // 标准化消息格式
  const normalizedMsg = normalizeMessage(message)
  
  // 关键修复：创建新的messages对象，确保Vue响应式更新
  const currentMessages = messages.value[key] || []
  messages.value = {
    ...messages.value,
    [key]: [...currentMessages, normalizedMsg]
  }
  
  addLog(`💾 消息已添加到列表 [${type}:${id}]，当前共${messages.value[key].length}条`, 'info')
  addLog(`🔍 当前聊天: ${currentChatType.value}:${currentChatId.value}`, 'info')
  addLog(`📝 消息内容预览: ${normalizedMsg.content?.substring(0, 30)}`, 'info')
  
  // 强制刷新界面
  refreshKey.value++
  addLog(`🔄 刷新界面 (key: ${refreshKey.value})`, 'info')
  
  // 如果不是当前聊天，增加未读数
  if (currentChatType.value !== type || currentChatId.value !== id) {
    unreadCount.value = {
      ...unreadCount.value,
      [key]: (unreadCount.value[key] || 0) + 1
    }
    addLog(`🔔 未读消息 +1 [${key}]，请点击联系人查看`, 'info')
  } else {
    addLog(`✅ 是当前聊天窗口，消息应该立即显示！`, 'success')
    addLog(`📊 当前消息总数: ${messages.value[key].length}`, 'info')
    addLog(`📊 computed消息数: ${getCurrentMessages.value.length}`, 'info')
    nextTick(() => {
      scrollToBottom()
      addLog(`📜 已滚动到底部`, 'info')
    })
  }
}

// 1. 登录
async function login() {
  try {
    loginError.value = ''
    
    // 验证输入
    if (!phoneNumber.value && !email.value) {
      loginError.value = '请至少输入手机号或邮箱'
      addLog('❌ 请至少输入手机号或邮箱', 'error')
      return
    }
    
    if (!password.value) {
      loginError.value = '请输入密码'
      addLog('❌ 请输入密码', 'error')
      return
    }
    
    if (password.value.length < 6) {
      loginError.value = '密码长度必须大于6位'
      addLog('❌ 密码长度必须大于6位', 'error')
      return
    }
    
    addLog('正在登录...')
    addLog(`请求: POST /api/Login/LoginByPhoneAndPassword`, 'info')
    addLog(`手机号: ${phoneNumber.value || '(空)'}, 邮箱: ${email.value || '(空)'}`, 'info')
    
    const response = await axios.post('/api/Login/LoginByPhoneAndPassword', {
      userBasic: {
        phoneNumber: phoneNumber.value || null,
        email: email.value || null
      },
      password: password.value
    })
    
    token.value = response.data.accessToken
    userId.value = response.data.userId
    userName.value = response.data.userName || response.data.userId
    
    localStorage.setItem('accessToken', token.value)
    localStorage.setItem('userId', userId.value)
    localStorage.setItem('userName', userName.value)
    
    // 显示调试日志
    showDebugLog.value = true
    
    addLog(`✅ 登录成功！${userName.value}`, 'success')
    addLog(`📌 用户ID: ${userId.value}`, 'info')
    addLog(`🔖 代码版本: v2.0 - 实时消息修复版`, 'info')
    addLog(`━━━━━━━━━━━━━━━━━━━━━━`, 'info')
    addLog(`🎯 下一步操作：`, 'info')
    addLog(`1️⃣ 点击顶部绿色按钮 "⚡ 立即连接" 连接到ChatHub`, 'info')
    addLog(`2️⃣ 等待状态变为 "✅ 在线"`, 'info')
    addLog(`3️⃣ 点击左侧 + 新建聊天，输入对方用户ID`, 'info')
    addLog(`4️⃣ 开始发送消息测试！`, 'info')
    addLog(`━━━━━━━━━━━━━━━━━━━━━━`, 'info')
    
    // 不自动连接，让用户明确看到需要点击连接按钮
    // setTimeout(() => connectSignalR(), 500)
  } catch (error) {
    // 详细的错误信息
    const errData = error.response?.data
    let errorMsg = ''
    
    if (typeof errData === 'string') {
      errorMsg = errData
    } else if (errData?.message) {
      errorMsg = errData.message
    } else if (errData?.title) {
      errorMsg = errData.title
    } else {
      errorMsg = error.message
    }
    
    loginError.value = errorMsg
    addLog(`❌ 登录失败 (${error.response?.status || 'Network'}): ${errorMsg}`, 'error')
    addLog(`完整错误: ${JSON.stringify(errData)}`, 'error')
  }
}

// 退出登录
function logout() {
  if (isConnected.value) {
    disconnectSignalR()
  }
  token.value = ''
  userId.value = ''
  userName.value = ''
  localStorage.clear()
  messages.value = {}
  onlineUsers.value = []
  joinedGroups.value = []
  currentChatId.value = ''
  currentChatType.value = ''
  addLog('已退出登录')
}

// 2. 连接SignalR
async function connectSignalR() {
  try {
    if (!token.value) {
      addLog('❌ 请先登录', 'error')
      return
    }

    addLog('正在连接ChatHub...')
    
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/chatservice/hubs/chat', {
        accessTokenFactory: () => token.value
      })
      .configureLogging(signalR.LogLevel.Information)
      .withAutomaticReconnect()
      .build()

    // 监听接收私聊消息
    connection.on('ReceivePrivateMessage', (message) => {
      const fromUser = message.fromUserId || message.FromUserId
      const content = message.content || message.Content
      
      addLog(`📨 收到私聊 [${fromUser}]: ${content}`, 'success')
      addLog(`消息详情: ${JSON.stringify(message).substring(0, 100)}`, 'info')
      
      addMessageToList('private', fromUser, message)
      
      // 自动添加发送者到聊天用户列表
      if (!chattedUsers.value.includes(fromUser)) {
        chattedUsers.value.push(fromUser)
      }
      
      // 强制滚动到底部
      scrollToBottom()
    })

    // 监听私聊发送确认
    connection.on('PrivateMessageSent', (message) => {
      const toUser = message.toUserId || message.ToUserId
      
      addLog(`✅ 私聊已发送 [to:${toUser}]`, 'success')
      addMessageToList('private', toUser, message)
      
      // 自动添加接收者到聊天用户列表
      if (!chattedUsers.value.includes(toUser)) {
        chattedUsers.value.push(toUser)
      }
      
      // 强制滚动到底部
      scrollToBottom()
    })

    // 监听接收群聊消息
    connection.on('ReceiveGroupMessage', (message) => {
      const fromUser = message.fromUserId || message.FromUserId
      const groupId = message.groupId || message.GroupId
      const content = message.content || message.Content
      
      addLog(`📢 群聊消息 [${fromUser}@${groupId}]: ${content}`, 'success')
      addMessageToList('group', groupId, message)
      
      // 强制滚动到底部
      scrollToBottom()
    })

    // 连接状态
    connection.onreconnecting(() => {
      addLog('⚠️ 正在重连...', 'error')
      isConnected.value = false
    })

    connection.onreconnected(() => {
      addLog('✅ 重连成功', 'success')
      isConnected.value = true
      getOnlineUsers()
    })

    connection.onclose(() => {
      addLog('⚪ 连接已关闭')
      isConnected.value = false
    })

    await connection.start()
    isConnected.value = true
    addLog('✅ 已连接到ChatHub！', 'success')
    
    // 自动刷新在线用户
    await getOnlineUsers()
    
    // 定期刷新在线用户
    setInterval(() => {
      if (isConnected.value) getOnlineUsers()
    }, 10000)
  } catch (error) {
    addLog(`❌ 连接失败: ${error.message}`, 'error')
    isConnected.value = false
  }
}

// 断开连接
async function disconnectSignalR() {
  if (connection) {
    await connection.stop()
    isConnected.value = false
    addLog('⚪ 已断开连接')
  }
}

// 3. 获取在线用户
async function getOnlineUsers() {
  try {
    const response = await axios.get('/chatservice/api/presence/online-users')
    onlineUsers.value = response.data
  } catch (error) {
    addLog(`❌ 获取在线用户失败: ${error.message}`, 'error')
  }
}

// 4. 打开私聊
async function openPrivateChat(targetUserId) {
  currentChatType.value = 'private'
  currentChatId.value = targetUserId
  clearUnread('private', targetUserId)
  
  // 加载历史消息（如果还没加载）
  const key = `private:${targetUserId}`
  if (!messages.value[key]) {
    await loadPrivateHistory(targetUserId)
  }
  
  scrollToBottom()
}

// 5. 打开群聊
function openGroupChat(groupId) {
  currentChatType.value = 'group'
  currentChatId.value = groupId
  clearUnread('group', groupId)
  scrollToBottom()
}

// 6. 开始新聊天（支持离线用户）
function startNewChat() {
  if (!newChatUserId.value.trim()) {
    addLog('❌ 请输入用户ID', 'error')
    return
  }
  
  if (newChatUserId.value === userId.value) {
    addLog('❌ 不能给自己发消息', 'error')
    return
  }
  
  const targetUserId = newChatUserId.value.trim()
  
  // 添加到聊天用户列表
  if (!chattedUsers.value.includes(targetUserId)) {
    chattedUsers.value.push(targetUserId)
  }
  
  showNewChatDialog.value = false
  newChatUserId.value = ''
  
  // 打开聊天窗口
  openPrivateChat(targetUserId)
  
  addLog(`💬 开始与 ${targetUserId} 聊天（${isUserOnline(targetUserId) ? '在线' : '离线'}）`)
}

// 7. 加入新群组
async function joinNewGroup() {
  try {
    if (!connection || !isConnected.value) {
      addLog('❌ 请先连接ChatHub', 'error')
      return
    }
    
    if (!newGroupId.value.trim()) {
      addLog('❌ 请输入群组ID', 'error')
      return
    }
    
    await connection.invoke('JoinGroup', newGroupId.value)
    
    if (!joinedGroups.value.includes(newGroupId.value)) {
      joinedGroups.value.push(newGroupId.value)
    }
    
    addLog(`✅ 已加入群组: ${newGroupId.value}`, 'success')
    showJoinGroupDialog.value = false
    
    // 打开群聊窗口
    openGroupChat(newGroupId.value)
    
    // 加载历史消息
    await loadGroupHistory(newGroupId.value)
  } catch (error) {
    addLog(`❌ 加入群组失败: ${error.message}`, 'error')
  }
}

// 离开当前群组
async function leaveCurrentGroup() {
  try {
    if (currentChatType.value !== 'group' || !currentChatId.value) return
    
    await connection.invoke('LeaveGroup', currentChatId.value)
    
    joinedGroups.value = joinedGroups.value.filter(g => g !== currentChatId.value)
    delete messages.value[`group:${currentChatId.value}`]
    
    addLog(`⚪ 已离开群组: ${currentChatId.value}`)
    
    currentChatId.value = ''
    currentChatType.value = ''
  } catch (error) {
    addLog(`❌ 离开群组失败: ${error.message}`, 'error')
  }
}

// 8. 发送消息
async function sendCurrentMessage() {
  if (!messageInput.value.trim()) {
    addLog('⚠️ 消息内容不能为空', 'warn')
    return
  }
  
  if (!isConnected.value) {
    addLog('❌ 请先连接ChatHub再发送消息', 'error')
    alert('请先点击顶部"连接"按钮连接到ChatHub')
    return
  }
  
  if (!currentChatId.value) {
    addLog('❌ 请先选择联系人', 'error')
    alert('请先从左侧选择一个联系人或群组')
    return
  }
  
  try {
    if (currentChatType.value === 'private') {
      await connection.invoke('SendPrivateMessage', currentChatId.value, messageInput.value)
      addLog(`📤 私聊已发送: ${messageInput.value.substring(0, 20)}${messageInput.value.length > 20 ? '...' : ''}`)
    } else if (currentChatType.value === 'group') {
      await connection.invoke('SendGroupMessage', currentChatId.value, messageInput.value)
      addLog(`📤 群聊已发送: ${messageInput.value.substring(0, 20)}${messageInput.value.length > 20 ? '...' : ''}`)
    }
    
    messageInput.value = ''
  } catch (error) {
    addLog(`❌ 发送失败: ${error.message}`, 'error')
    alert(`发送失败：${error.message}\n\n请检查：\n1. 是否连接到ChatHub\n2. 网络连接是否正常\n3. 查看调试日志了解详情`)
  }
}

// 9. 加载私聊历史
async function loadPrivateHistory(targetUserId) {
  try {
    if (!token.value) {
      addLog('❌ Token不存在，无法加载历史', 'error')
      return
    }
    
    addLog(`📜 正在加载私聊历史: ${targetUserId}`)
    addLog(`Token前缀: ${token.value.substring(0, 20)}...`, 'info')
    
    const response = await axios.get(
      `/chatservice/api/messages/private/${targetUserId}?pageSize=50`,
      { 
        headers: { 
          Authorization: `Bearer ${token.value}`,
          'Content-Type': 'application/json'
        } 
      }
    )
    
    const key = `private:${targetUserId}`
    // 标准化所有消息对象并更新（触发响应式）
    const historyMessages = (response.data || []).map(msg => normalizeMessage(msg))
    messages.value = {
      ...messages.value,
      [key]: historyMessages
    }
    addLog(`✅ 加载私聊历史: ${historyMessages.length}条`, 'success')
    nextTick(() => scrollToBottom())
  } catch (error) {
    const status = error.response?.status
    const errMsg = error.response?.data || error.message
    
    addLog(`❌ 加载私聊历史失败 (${status}): ${JSON.stringify(errMsg)}`, 'error')
    
    // 如果是401，给出提示
    if (status === 401) {
      addLog('💡 提示: Token可能已过期或无效，请重新登录', 'error')
    }
    
    // 初始化为空数组，避免重复加载（触发响应式）
    const key = `private:${targetUserId}`
    if (!messages.value[key]) {
      messages.value = {
        ...messages.value,
        [key]: []
      }
    }
  }
}

// 加载群聊历史
async function loadGroupHistory(groupId) {
  try {
    if (!token.value) {
      addLog('❌ Token不存在，无法加载历史', 'error')
      return
    }
    
    addLog(`📜 正在加载群聊历史: ${groupId}`)
    
    const response = await axios.get(
      `/chatservice/api/messages/group/${groupId}?pageSize=50`,
      { 
        headers: { 
          Authorization: `Bearer ${token.value}`,
          'Content-Type': 'application/json'
        } 
      }
    )
    
    const key = `group:${groupId}`
    // 标准化所有消息对象并更新（触发响应式）
    const historyMessages = (response.data || []).map(msg => normalizeMessage(msg))
    messages.value = {
      ...messages.value,
      [key]: historyMessages
    }
    addLog(`✅ 加载群聊历史: ${historyMessages.length}条`, 'success')
    nextTick(() => scrollToBottom())
  } catch (error) {
    const status = error.response?.status
    const errMsg = error.response?.data || error.message
    
    addLog(`❌ 加载群聊历史失败 (${status}): ${JSON.stringify(errMsg)}`, 'error')
    
    if (status === 401) {
      addLog('💡 提示: Token可能已过期或无效，请重新登录', 'error')
    }
    
    // 初始化为空数组
    const key = `group:${groupId}`
    if (!messages.value[key]) {
      messages.value[key] = []
    }
  }
}

// 页面加载时恢复状态（如果需要自动登录，取消下面的注释）
// const savedToken = localStorage.getItem('accessToken')
// const savedUserId = localStorage.getItem('userId')
// const savedUserName = localStorage.getItem('userName')
// if (savedToken) {
//   token.value = savedToken
//   userId.value = savedUserId
//   userName.value = savedUserName || savedUserId
//   addLog('已从缓存恢复登录状态')
// }
</script>

<style scoped>
.chat-app {
  width: 100vw;
  height: 100vh;
  overflow: hidden;
  background: #f0f2f5;
}

/* 登录界面 */
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-box {
  background: white;
  padding: 40px;
  border-radius: 10px;
  box-shadow: 0 10px 40px rgba(0,0,0,0.2);
  width: 400px;
}

.login-box h2 {
  margin: 0 0 20px 0;
  text-align: center;
  color: #333;
}

.tips-box {
  background: #e3f2fd;
  border-left: 3px solid #2196f3;
  padding: 12px;
  margin-bottom: 20px;
  border-radius: 4px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  color: #666;
  font-size: 14px;
}

.input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 14px;
  box-sizing: border-box;
}

.input:focus {
  outline: none;
  border-color: #667eea;
}

.btn-login {
  width: 100%;
  padding: 12px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 5px;
  font-size: 16px;
  cursor: pointer;
  margin-top: 10px;
}

.btn-login:hover {
  opacity: 0.9;
}

.error-message {
  margin-top: 15px;
  padding: 10px;
  background: #fee;
  color: #c33;
  border-radius: 5px;
  font-size: 14px;
}

/* 聊天主界面 */
.chat-main {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

/* 警告横幅 */
.warning-banner {
  background: #fff3cd;
  border-bottom: 1px solid #ffc107;
  padding: 12px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  color: #856404;
}

.warning-banner code {
  background: rgba(0,0,0,0.1);
  padding: 2px 6px;
  border-radius: 3px;
  font-size: 12px;
}

.btn-close-banner {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #856404;
  padding: 0;
  width: 24px;
  height: 24px;
  line-height: 1;
}

.btn-close-banner:hover {
  opacity: 0.7;
}

.top-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background: white;
  border-bottom: 1px solid #e0e0e0;
  box-shadow: 0 2px 5px rgba(0,0,0,0.05);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 16px;
}

.user-details {
  display: flex;
  flex-direction: column;
}

.user-name {
  font-weight: bold;
  color: #333;
}

.user-status {
  font-size: 12px;
  color: #666;
  display: flex;
  align-items: center;
  gap: 5px;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.status-dot.online {
  background: #4caf50;
}

.status-dot.offline {
  background: #999;
}

.top-actions {
  display: flex;
  gap: 10px;
}

.btn-sm {
  padding: 6px 12px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
}

.btn-sm:hover {
  background: #5568d3;
}

.btn-danger {
  background: #f44336;
}

.btn-danger:hover {
  background: #d32f2f;
}

.btn-log {
  background: #ff9800 !important;
}

.btn-log:hover {
  background: #f57c00 !important;
}

.btn-connect-big {
  padding: 10px 30px;
  background: linear-gradient(135deg, #4caf50 0%, #45a049 100%);
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;
  font-weight: bold;
  box-shadow: 0 3px 10px rgba(76, 175, 80, 0.3);
  transition: all 0.3s;
  animation: pulse 2s infinite;
}

.btn-connect-big:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(76, 175, 80, 0.4);
}

@keyframes pulse {
  0%, 100% {
    box-shadow: 0 3px 10px rgba(76, 175, 80, 0.3);
  }
  50% {
    box-shadow: 0 5px 20px rgba(76, 175, 80, 0.6);
  }
}

/* 主容器 */
.main-container {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 70px); /* 减去顶部栏高度 */
}

/* 左侧栏 */
.sidebar {
  width: 280px;
  background: white;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
}

.sidebar-header {
  padding: 15px;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.sidebar-header h3 {
  margin: 0;
  font-size: 18px;
  color: #333;
}

.btn-icon {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 18px;
  padding: 5px;
}

.btn-icon:hover {
  opacity: 0.7;
}

.contact-list {
  flex: 1;
  overflow-y: auto;
}

.list-section-title {
  padding: 10px 15px;
  font-size: 12px;
  color: #999;
  font-weight: bold;
  background: #f5f5f5;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.btn-icon-sm {
  background: #667eea;
  color: white;
  border: none;
  border-radius: 50%;
  width: 20px;
  height: 20px;
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-icon-sm:hover {
  background: #5568d3;
}

.online-dot {
  color: #4caf50;
  font-size: 10px;
  margin-right: 3px;
}

.contact-item {
  display: flex;
  align-items: center;
  padding: 12px 15px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  transition: background 0.2s;
  position: relative;
}

.contact-item:hover {
  background: #f5f5f5;
}

.contact-item.active {
  background: #e3f2fd;
}

.contact-item.add-btn {
  color: #667eea;
}

.contact-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #e3f2fd;
  color: #667eea;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 12px;
  flex-shrink: 0;
}

.contact-avatar.group {
  background: #fff3e0;
  color: #ff9800;
  font-size: 18px;
}

.contact-info {
  flex: 1;
  min-width: 0;
}

.contact-name {
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.contact-status {
  font-size: 12px;
  color: #999;
}

.unread-badge {
  background: #f44336;
  color: white;
  border-radius: 10px;
  padding: 2px 8px;
  font-size: 12px;
  font-weight: bold;
  min-width: 18px;
  text-align: center;
}

.empty-tip {
  padding: 20px;
  text-align: center;
  color: #999;
  font-size: 13px;
}

/* 聊天面板 */
.chat-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: #f5f5f5;
}

.chat-header {
  padding: 15px 20px;
  background: white;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.chat-title {
  font-size: 16px;
  font-weight: bold;
  color: #333;
}

.chat-subtitle {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.chat-title-empty {
  color: #999;
  font-size: 15px;
}

.chat-actions {
  display: flex;
  gap: 10px;
}

/* 消息容器 */
.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.welcome-screen {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #999;
}

.welcome-icon {
  font-size: 64px;
  margin-bottom: 20px;
}

.welcome-screen h2 {
  color: #666;
  margin: 0 0 10px 0;
}

.welcome-screen p {
  margin: 0 0 30px 0;
}

.welcome-tips {
  background: white;
  padding: 20px;
  border-radius: 10px;
  text-align: left;
  font-size: 14px;
  line-height: 1.8;
}

.messages-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

/* 消息项 */
.message-wrapper {
  display: flex;
  gap: 10px;
  max-width: 70%;
}

.message-wrapper.mine {
  flex-direction: row-reverse;
  margin-left: auto;
}

.message-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #e3f2fd;
  color: #667eea;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 14px;
  flex-shrink: 0;
}

.message-wrapper.mine .message-avatar {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.message-box {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.message-wrapper.mine .message-box {
  align-items: flex-end;
}

.message-sender {
  font-size: 12px;
  color: #666;
  margin-bottom: 2px;
}

.message-bubble {
  background: white;
  padding: 10px 15px;
  border-radius: 10px;
  word-wrap: break-word;
  box-shadow: 0 1px 2px rgba(0,0,0,0.1);
  line-height: 1.4;
}

.message-wrapper.mine .message-bubble {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.message-time {
  font-size: 11px;
  color: #999;
}

/* 输入区域 */
.input-container {
  display: flex;
  gap: 10px;
  padding: 15px 20px;
  background: white;
  border-top: 1px solid #e0e0e0;
}

.offline-tip {
  padding: 8px 20px;
  background: #fff3cd;
  border-top: 1px solid #ffc107;
  font-size: 12px;
  color: #856404;
  text-align: center;
}

.message-input {
  flex: 1;
  padding: 10px 15px;
  border: 1px solid #ddd;
  border-radius: 20px;
  font-size: 14px;
  outline: none;
}

.message-input:focus {
  border-color: #667eea;
}

.message-input:disabled {
  background: #f5f5f5;
  cursor: not-allowed;
}

.btn-send {
  padding: 10px 25px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 20px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
}

.btn-send:hover:not(:disabled) {
  opacity: 0.9;
}

.btn-send:disabled {
  background: #ccc;
  cursor: not-allowed;
}

/* 模态框 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-box {
  background: white;
  padding: 25px;
  border-radius: 10px;
  width: 400px;
  box-shadow: 0 10px 40px rgba(0,0,0,0.3);
}

.modal-box h3 {
  margin: 0 0 20px 0;
  color: #333;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.btn-primary {
  padding: 8px 20px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

.btn-primary:hover {
  background: #5568d3;
}

.btn-cancel {
  padding: 8px 20px;
  background: #f5f5f5;
  color: #666;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

.btn-cancel:hover {
  background: #e0e0e0;
}

/* 侧边栏提示 */
.sidebar-tip {
  padding: 15px;
  margin: 10px;
  border-radius: 8px;
  text-align: center;
  font-size: 13px;
  line-height: 1.6;
}

.sidebar-tip.warning {
  background: #fff3cd;
  border: 1px solid #ffc107;
  color: #856404;
}

/* 连接警告 */
.connection-warning {
  padding: 30px;
  background: #fff3cd;
  border: 2px solid #ffc107;
  border-radius: 10px;
  margin: 20px 0;
}

.warning-icon {
  font-size: 64px;
  margin-bottom: 10px;
}

.connection-warning h3 {
  color: #856404;
  margin: 10px 0;
}

.connection-warning p {
  color: #856404;
  font-size: 16px;
  margin: 10px 0;
}

.btn-primary-big {
  padding: 12px 40px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 16px;
  font-weight: bold;
  cursor: pointer;
  margin-top: 15px;
  transition: all 0.3s;
}

.btn-primary-big:hover {
  background: #5568d3;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

/* 调试面板 */
.debug-panel {
  position: fixed;
  right: 20px;
  bottom: 20px;
  width: 400px;
  height: 300px;
  background: white;
  border-radius: 10px;
  box-shadow: 0 5px 20px rgba(0,0,0,0.3);
  display: flex;
  flex-direction: column;
  z-index: 1000;
}

.debug-header {
  padding: 10px 15px;
  background: #f5f5f5;
  border-bottom: 1px solid #e0e0e0;
  border-radius: 10px 10px 0 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: bold;
  color: #333;
}

.btn-close {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #999;
  padding: 0;
  width: 24px;
  height: 24px;
  line-height: 1;
}

.btn-close:hover {
  color: #333;
}

.debug-content {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
  font-family: 'Courier New', monospace;
  font-size: 12px;
}

.log-item {
  padding: 5px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  gap: 10px;
}

.log-time {
  color: #999;
  white-space: nowrap;
}

.log-info {
  color: #333;
}

.log-success {
  color: #4caf50;
}

.log-error {
  color: #f44336;
}

/* 滚动条样式 */
.contact-list::-webkit-scrollbar,
.messages-container::-webkit-scrollbar,
.debug-content::-webkit-scrollbar {
  width: 6px;
}

.contact-list::-webkit-scrollbar-track,
.messages-container::-webkit-scrollbar-track,
.debug-content::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.contact-list::-webkit-scrollbar-thumb,
.messages-container::-webkit-scrollbar-thumb,
.debug-content::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 3px;
}

.contact-list::-webkit-scrollbar-thumb:hover,
.messages-container::-webkit-scrollbar-thumb:hover,
.debug-content::-webkit-scrollbar-thumb:hover {
  background: #555;
}
</style>
