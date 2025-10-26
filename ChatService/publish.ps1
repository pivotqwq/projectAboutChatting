# ChatService 发布脚本

Write-Host "正在发布 ChatService..." -ForegroundColor Green

# 清理之前的发布文件
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}

# 发布项目
dotnet publish -c Release -o publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ 发布成功！" -ForegroundColor Green
    Write-Host ""
    Write-Host "发布目录: publish/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "接下来请执行以下步骤：" -ForegroundColor Cyan
    Write-Host "1. 停止服务器上的ChatService应用"
    Write-Host "2. 上传 publish 目录内容到服务器: /www/wwwroot/backend/chatService/chat_publish"
    Write-Host "3. 重启ChatService应用"
    Write-Host ""
} else {
    Write-Host "❌ 发布失败！" -ForegroundColor Red
}

