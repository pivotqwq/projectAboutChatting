# 使用官方.NET 8运行时镜像作为基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 使用官方.NET 8 SDK镜像进行构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 复制项目文件并还原依赖
COPY ["ApiGateway.csproj", "./"]
RUN dotnet restore "./ApiGateway.csproj"

# 复制所有源代码
COPY . .
WORKDIR "/src/."

# 构建应用程序
RUN dotnet build "./ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 发布应用程序
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 最终镜像
FROM base AS final
WORKDIR /app

# 创建日志目录
RUN mkdir -p /app/logs

# 从发布阶段复制应用程序
COPY --from=publish /app/publish .

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/api/aggregated/health || exit 1

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
