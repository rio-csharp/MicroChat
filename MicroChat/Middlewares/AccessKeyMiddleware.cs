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
        // 1. 检查 Header 是否存在
        if (!context.Request.Headers.TryGetValue("X-Access-Key", out var originalCode))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Missing Access Key");
            return;
        }

        // 2. 获取配置 (假设格式为 "key1,key2,key3")
        var validCodesString = _configuration["AccessKey"];

        // 3. 验证逻辑
        // 如果配置为空，或者 分割后的key列表中不包含用户提供的code
        if (string.IsNullOrEmpty(validCodesString) ||
            !validCodesString.Split(',').Any(code => code.Trim() == originalCode))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Invalid Access Key");
            return;
        }

        await _next(context);
    }

}
