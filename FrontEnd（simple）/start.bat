@echo off
chcp 65001
echo.
echo ========================================
echo   启动 ChatService 测试前端
echo ========================================
echo.

echo 正在安装依赖...
call npm install

echo.
echo 正在启动开发服务器...
echo 访问地址: http://localhost:5173/chat-test
echo.

call npm run dev

