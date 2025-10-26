#!/bin/bash

# ChatService 生产环境启动脚本

echo "🚀 启动 ChatService 生产环境..."

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker 未运行，请先启动 Docker"
    exit 1
fi

# 检查环境变量
if [ -z "$JWT_SECRET_KEY" ]; then
    echo "⚠️  警告: JWT_SECRET_KEY 环境变量未设置，使用默认值"
fi

if [ -z "$MONGO_CONNECTION" ]; then
    echo "⚠️  警告: MONGO_CONNECTION 环境变量未设置，使用默认值"
fi

if [ -z "$REDIS_CONNECTION" ]; then
    echo "⚠️  警告: REDIS_CONNECTION 环境变量未设置，使用默认值"
fi

# 创建必要的目录
mkdir -p logs
mkdir -p mongo-init

# 生成 MongoDB 初始化脚本
cat > mongo-init/init.js << EOF
db = db.getSiblingDB('chatdb');

// 创建用户
db.createUser({
  user: 'chatuser',
  pwd: 'chatpass123',
  roles: [
    {
      role: 'readWrite',
      db: 'chatdb'
    }
  ]
});

// 创建集合和索引
db.createCollection('messages');
db.createCollection('conversations');
db.createCollection('users');

// 创建索引
db.messages.createIndex({ conversationId: 1, timestamp: -1 });
db.messages.createIndex({ senderId: 1 });
db.conversations.createIndex({ participants: 1 });
db.users.createIndex({ username: 1 }, { unique: true });
db.users.createIndex({ email: 1 }, { unique: true });

print('Database initialized successfully');
EOF

# 启动所有服务
echo "📦 启动所有服务..."
docker-compose up -d

# 等待服务启动
echo "⏳ 等待服务启动..."
sleep 15

# 检查服务状态
echo "🔍 检查服务状态..."

# 检查 ChatService
if ! curl -f http://localhost:9391/health > /dev/null 2>&1; then
    echo "❌ ChatService 健康检查失败"
    docker-compose logs chatservice
    exit 1
fi

# 检查 MongoDB
if ! docker exec chatservice-mongo mongosh --eval "db.runCommand('ping')" > /dev/null 2>&1; then
    echo "❌ MongoDB 健康检查失败"
    exit 1
fi

# 检查 Redis
if ! docker exec chatservice-redis redis-cli ping > /dev/null 2>&1; then
    echo "❌ Redis 健康检查失败"
    exit 1
fi

echo "✅ 所有服务健康检查通过"

# 显示服务信息
echo ""
echo "🎉 生产环境启动完成！"
echo "📱 应用地址: http://localhost:9391"
echo "📚 API 文档: http://localhost:9391/swagger"
echo "🗄️  MongoDB 管理: http://localhost:8081 (admin/password123)"
echo "🔴 Redis 管理: http://localhost:8082"
echo ""
echo "📊 服务状态:"
docker-compose ps

echo ""
echo "📋 常用命令:"
echo "  查看日志: docker-compose logs -f"
echo "  停止服务: docker-compose down"
echo "  重启服务: docker-compose restart"
echo "  更新服务: docker-compose pull && docker-compose up -d"
