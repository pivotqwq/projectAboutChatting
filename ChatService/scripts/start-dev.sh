#!/bin/bash

# ChatService 开发环境启动脚本

echo "🚀 启动 ChatService 开发环境..."

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker 未运行，请先启动 Docker"
    exit 1
fi

# 检查 .NET SDK 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK 未安装，请先安装 .NET 8.0 SDK"
    exit 1
fi

# 创建必要的目录
mkdir -p logs
mkdir -p mongo-init

# 启动开发环境服务
echo "📦 启动数据库服务..."
docker-compose -f docker-compose.dev.yml up -d mongo-dev redis-dev

# 等待数据库启动
echo "⏳ 等待数据库启动..."
sleep 10

# 检查数据库连接
echo "🔍 检查数据库连接..."
if ! docker exec chatservice-mongo-dev mongosh --eval "db.runCommand('ping')" > /dev/null 2>&1; then
    echo "❌ MongoDB 连接失败"
    exit 1
fi

if ! docker exec chatservice-redis-dev redis-cli ping > /dev/null 2>&1; then
    echo "❌ Redis 连接失败"
    exit 1
fi

echo "✅ 数据库连接正常"

# 还原 NuGet 包
echo "📦 还原 NuGet 包..."
dotnet restore

# 构建项目
echo "🔨 构建项目..."
dotnet build

# 启动应用
echo "🎯 启动 ChatService 应用..."
dotnet run --environment Development

echo "🎉 开发环境启动完成！"
echo "📱 应用地址: http://localhost:9391"
echo "📚 API 文档: http://localhost:9391/swagger"
echo "🗄️  MongoDB 管理: http://localhost:8083"
echo "🔴 Redis 管理: http://localhost:8084"
