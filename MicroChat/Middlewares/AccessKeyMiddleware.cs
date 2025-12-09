using System.Net;

namespace MicroChat.Middlewares;

public class AccessKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    public AccessKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }
    public async Task Invoke(HttpContext context)
    {
        // 1. 获取正确的访问码
        var correctCode = _configuration["AccessKey"];
        // 2. 获取客户端传来的 Header (约定 key 为 "X-Access-Key")
        if (!context.Request.Headers.TryGetValue("X-Access-Key", out var originalCode))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Missing Access Key");
            return;
        }
        // 3. 校验比对
        if (originalCode != correctCode)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Invalid Access Key");
            return;
        }
        await _next(context);
    }
}
