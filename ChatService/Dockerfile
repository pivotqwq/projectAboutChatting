# 使用官方的 .NET 8.0 运行时镜像作为基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 使用官方的 .NET 8.0 SDK 镜像作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖
COPY ["ChatService.csproj", "./"]
RUN dotnet restore "ChatService.csproj"

# 复制所有源代码
COPY . .

# 构建应用程序
WORKDIR "/src"
RUN dotnet build "ChatService.csproj" -c Release -o /app/build

# 发布应用程序
FROM build AS publish
RUN dotnet publish "ChatService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 创建最终运行时镜像
FROM base AS final
WORKDIR /app

# 复制发布的应用程序
COPY --from=publish /app/publish .

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# 创建非 root 用户
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# 启动应用程序
ENTRYPOINT ["dotnet", "ChatService.dll"]
