<template>
  <div class="login-container">
    <div class="login-form">
      <h2>用户登录</h2>
      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label for="phoneNumber">手机号</label>
          <input 
            type="text" 
            id="phoneNumber" 
            v-model="phoneNumber" 
            required 
            placeholder="请输入手机号"
          />
        </div>
        <div class="form-group">
          <label for="email">邮箱</label>
          <input 
            type="text" 
            id="email" 
            v-model="email" 
            required 
            placeholder="请输入邮箱"
          />
        </div>
        <div class="form-group">
          <label for="password">密码</label>
          <input 
            type="password" 
            id="password" 
            v-model="password" 
            required 
            placeholder="请输入密码（长度必须大于6）"
          />
        </div>
        <button type="submit" class="login-btn" :disabled="isLoading">
          <span v-if="isLoading">登录中...</span>
          <span v-else>登录</span>
        </button>
      </form>
      <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
    </div>
  </div>
</template>

<script>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

export default {
  name: 'LoginView',
  setup() {
    const phoneNumber = ref('')
    const email = ref('')
    const password = ref('')
    const errorMessage = ref('')
    const isLoading = ref(false)
    const router = useRouter()

    // 设置axios基础URL为相对路径，通过Vite代理发送请求
    axios.defaults.baseURL = '/api'//fetch
    axios.defaults.timeout = 10000

    const handleLogin = async () => {
      // 清除之前的错误信息
      errorMessage.value = ''

      // 所有验证规则已注释掉，直接进行API调用

      try {
        isLoading.value = true
        
        // 构建请求参数
        const requestData = {
          userBasic: {
            phoneNumber: phoneNumber.value,
            email: email.value
          },
          password: password.value
        }

        // 调用登录API - 添加前导斜杠确保路径格式正确
        const response = await axios.post('/Login/LoginByPhoneAndPassword', requestData)
        
        // 处理登录成功响应
        if (response.status === 200) {
          const { accessToken, refreshToken, tokenType, expiresIn, userId, userName } = response.data
          
          // 存储登录信息到localStorage
          localStorage.setItem('accessToken', accessToken)
          localStorage.setItem('refreshToken', refreshToken)
          localStorage.setItem('tokenType', tokenType)
          localStorage.setItem('expiresIn', expiresIn)
          localStorage.setItem('userId', userId)
          localStorage.setItem('userName', userName)
          localStorage.setItem('isLoggedIn', 'true')
          
          // 打印日志确认代码执行
          console.log('登录成功，准备跳转到welcome页面')
          
          // 跳转到welcome页面
          setTimeout(() => {
            window.location.href = '/welcome'
          }, 100)
        }
      } catch (error) {
        // 处理错误响应
        if (error.response) {
          // 服务器返回了错误状态码
          console.log('API错误详情:', error.response)
          switch (error.response.status) {
            case 400:
              errorMessage.value = error.response.data || '账号或者密码错误'
              break
            case 401:
              errorMessage.value = '认证失败，请重新登录'
              break
            case 405:
              errorMessage.value = '请求方法不允许，请检查API接口配置'
              break
            default:
              errorMessage.value = `服务器错误 (${error.response.status})，请稍后再试`
          }
        } else if (error.request) {
          // 请求发出但没有收到响应
          errorMessage.value = '网络错误，请检查您的网络连接'
        } else {
          // 请求配置出错
          errorMessage.value = '请求错误，请稍后再试'
        }
      } finally {
        isLoading.value = false
        
        // 3秒后清除错误信息
        if (errorMessage.value) {
          setTimeout(() => {
            errorMessage.value = ''
          }, 3000)
        }
      }
    }

    return {
      phoneNumber,
      email,
      password,
      errorMessage,
      isLoading,
      handleLogin
    }
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.login-form {
  background: white;
  padding: 40px;
  border-radius: 8px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
}

.login-form h2 {
  margin-bottom: 30px;
  text-align: center;
  color: #333;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  color: #555;
  font-weight: 500;
}

.form-group input {
  width: 100%;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 16px;
  transition: border-color 0.3s;
}

.form-group input:focus {
  outline: none;
  border-color: #667eea;
}

.login-btn {
  width: 100%;
  padding: 12px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  cursor: pointer;
  transition: background-color 0.3s;
}

.login-btn:hover {
  background: #5a67d8;
}

.error-message {
  color: #e53e3e;
  text-align: center;
  margin-top: 15px;
}
</style>