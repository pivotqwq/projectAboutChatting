#!/bin/bash

# API Gateway 开发环境启动脚本

echo "Starting API Gateway in Development Mode..."

# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5000

# 创建日志目录
mkdir -p logs

# 启动应用
echo "Launching API Gateway on http://localhost:5000"
dotnet run

echo "API Gateway started successfully!"
echo "Swagger UI: http://localhost:5000/swagger"
echo "Health Check: http://localhost:5000/api/aggregated/health"
