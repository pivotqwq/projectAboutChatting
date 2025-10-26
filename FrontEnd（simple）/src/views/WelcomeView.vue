<template>
  <div class="welcome-container">
    <div class="welcome-content">
      <h1>欢迎回来，{{ username }}！</h1>
      <p>您已成功登录系统。</p>
      <div class="features">
        <div class="feature-card">
          <h3>用户中心</h3>
          <p>管理您的个人信息和偏好设置</p>
        </div>
        <div class="feature-card">
          <h3>控制面板</h3>
          <p>查看和管理您的系统数据</p>
        </div>
        <div class="feature-card">
          <h3>帮助支持</h3>
          <p>获取系统使用指南和技术支持</p>
        </div>
      </div>
      <button @click="handleLogout" class="logout-btn">退出登录</button>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

export default {
  name: 'WelcomeView',
  setup() {
    const username = ref('')
    const router = useRouter()

    onMounted(() => {
      // 检查登录状态
      const isLoggedIn = localStorage.getItem('isLoggedIn') === 'true'
      // 从localStorage获取用户名（注意键名是userName，与LoginView.vue中保持一致）
      const storedUsername = localStorage.getItem('userName')
      
      if (isLoggedIn && storedUsername) {
        username.value = storedUsername
        console.log('验证通过，欢迎页面加载成功')
      } else {
        // 如果没有登录状态或用户名，重定向到登录页
        console.log('未找到有效登录状态或用户名，重定向到登录页')
        router.push('/login')
      }
    })

    const handleLogout = () => {
      // 清除所有登录相关信息
      localStorage.removeItem('isLoggedIn')
      localStorage.removeItem('username')
      localStorage.removeItem('accessToken')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('tokenType')
      localStorage.removeItem('expiresIn')
      localStorage.removeItem('userId')
      // 重定向到登录页
      router.push('/login')
    }

    return {
      username,
      handleLogout
    }
  }
}
</script>

<style scoped>
.welcome-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: #f5f7fa;
  padding: 20px;
}

.welcome-content {
  background: white;
  padding: 60px 40px;
  border-radius: 8px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.05);
  text-align: center;
  width: 100%;
  max-width: 800px;
}

.welcome-content h1 {
  color: #333;
  margin-bottom: 20px;
  font-size: 2.5em;
}

.welcome-content p {
  color: #666;
  margin-bottom: 40px;
  font-size: 1.1em;
}

.features {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 40px;
}

.feature-card {
  background: #f8fafc;
  padding: 20px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  transition: transform 0.3s, box-shadow 0.3s;
}

.feature-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 15px 30px rgba(0, 0, 0, 0.1);
}

.feature-card h3 {
  color: #333;
  margin-bottom: 10px;
}

.feature-card p {
  color: #666;
  font-size: 0.95em;
  margin-bottom: 0;
}

.logout-btn {
  padding: 12px 30px;
  background: #e53e3e;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  cursor: pointer;
  transition: background-color 0.3s;
}

.logout-btn:hover {
  background: #c53030;
}
</style>