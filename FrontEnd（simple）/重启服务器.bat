@echo off
chcp 65001
echo.
echo ========================================
echo   重启开发服务器
echo ========================================
echo.
echo 正在停止所有Node进程...
taskkill /F /IM node.exe >nul 2>&1

echo 等待3秒...
timeout /t 3 /nobreak >nul

echo.
echo 正在启动开发服务器...
echo ========================================
echo.

cd /d "%~dp0"
npm run dev

pause


