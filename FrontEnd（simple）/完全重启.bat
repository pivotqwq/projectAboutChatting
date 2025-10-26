@echo off
chcp 65001
cls
echo.
echo ========================================
echo   彻底重启开发服务器（强制清除缓存）
echo ========================================
echo.

echo [1/4] 停止所有Node进程...
taskkill /F /IM node.exe >nul 2>&1

echo [2/4] 清除npm缓存...
cd /d "%~dp0"
rd /s /q .vite 2>nul
rd /s /q dist 2>nul
rd /s /q node_modules\.vite 2>nul

echo [3/4] 等待3秒...
timeout /t 3 /nobreak >nul

echo [4/4] 启动开发服务器...
echo ========================================
echo.
echo 请打开浏览器访问: http://localhost:5174?v=%RANDOM%
echo 刷新时请按: Ctrl + Shift + R (硬刷新)
echo.
echo ========================================
echo.

npm run dev

pause

