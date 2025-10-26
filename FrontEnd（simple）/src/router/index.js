import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import WelcomeView from '../views/WelcomeView.vue'
import ChatTest from '../views/ChatTest.vue'

const routes = [
  {
    path: '/',
    redirect: '/chat-test'
  },
  {
    path: '/login',
    name: 'login',
    component: LoginView
  },
  {
    path: '/welcome',
    name: 'welcome',
    component: WelcomeView,
    meta: {
      requiresAuth: true
    }
  },
  {
    path: '/chat-test',
    name: 'chat-test',
    component: ChatTest
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫，检查是否需要登录
router.beforeEach((to, from, next) => {
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  const isLoggedIn = localStorage.getItem('isLoggedIn') === 'true'
  const accessToken = localStorage.getItem('accessToken')

  // 检查是否需要认证并且用户是否已登录且有有效的accessToken
  if (requiresAuth && (!isLoggedIn || !accessToken)) {
    // 清除可能存在的无效登录状态
    localStorage.removeItem('isLoggedIn')
    localStorage.removeItem('username')
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('tokenType')
    localStorage.removeItem('expiresIn')
    localStorage.removeItem('userId')
    
    next('/login')
  } else {
    next()
  }
})

export default router