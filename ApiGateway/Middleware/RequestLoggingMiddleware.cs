using System.Diagnostics;
using System.Text;

namespace ApiGateway.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // 添加请求ID到响应头
        context.Response.Headers.Add("X-Request-Id", requestId);

        // 记录请求开始
        LogRequest(context, requestId);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request {RequestId} failed with exception", requestId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);
        }
    }

    private void LogRequest(HttpContext context, string requestId)
    {
        var userId = context.Items["UserId"]?.ToString() ?? "anonymous";
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
        var referer = context.Request.Headers["Referer"].FirstOrDefault() ?? "direct";
        
        _logger.LogInformation(
            "Request {RequestId} started: {Method} {Path} from {UserId} (User-Agent: {UserAgent}, Referer: {Referer})",
            requestId,
            context.Request.Method,
            context.Request.Path,
            userId,
            userAgent,
            referer
        );
    }

    private void LogResponse(HttpContext context, string requestId, long elapsedMs)
    {
        var userId = context.Items["UserId"]?.ToString() ?? "anonymous";
        var statusCode = context.Response.StatusCode;
        var contentLength = context.Response.ContentLength ?? 0;

        var logLevel = statusCode switch
        {
            >= 200 and < 300 => LogLevel.Information,
            >= 300 and < 400 => LogLevel.Information,
            >= 400 and < 500 => LogLevel.Warning,
            >= 500 => LogLevel.Error,
            _ => LogLevel.Warning
        };

        _logger.Log(
            logLevel,
            "Request {RequestId} completed: {Method} {Path} -> {StatusCode} in {ElapsedMs}ms (User: {UserId}, Size: {ContentLength} bytes)",
            requestId,
            context.Request.Method,
            context.Request.Path,
            statusCode,
            elapsedMs,
            userId,
            contentLength
        );

        // 记录慢请求
        if (elapsedMs > 5000) // 超过5秒的请求
        {
            _logger.LogWarning(
                "Slow request detected: {RequestId} took {ElapsedMs}ms for {Method} {Path}",
                requestId,
                elapsedMs,
                context.Request.Method,
                context.Request.Path
            );
        }
    }
}
