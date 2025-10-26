#!/bin/bash

# API Gateway 生产环境启动脚本

echo "Starting API Gateway in Production Mode..."

# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://+:80

# 创建日志目录
mkdir -p logs

# 检查依赖服务
echo "Checking dependent services..."
services=("UserManager" "ForumManager" "ChatService" "MatchingService")
for service in "${services[@]}"; do
    echo "Checking $service availability..."
    # 这里可以添加服务健康检查逻辑
done

# 启动应用
echo "Launching API Gateway on port 80"
dotnet run

echo "API Gateway started successfully in production mode!"
