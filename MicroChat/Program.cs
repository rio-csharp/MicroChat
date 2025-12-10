using MicroChat.Components;
using MicroChat.Middlewares; 
using Microsoft.AspNetCore.Authentication.Cookies;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// --- 服务配置 ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDataProtection();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers();

// YARP配置保持不变，但去掉 AuthorizationPolicy，因为我们用中间件手动控制
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiKey = builder.Configuration["ApiKey"];
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        new[] { new RouteConfig { RouteId = "api-proxy", ClusterId = "api-cluster", Match = new RouteMatch { Path = "/api/proxy/{**catch-all}" } } },
        new[] { new ClusterConfig { ClusterId = "api-cluster", Destinations = new Dictionary<string, DestinationConfig> { { "destination1", new DestinationConfig { Address = string.IsNullOrWhiteSpace(apiBaseUrl) ? "https://api.openai.com" : apiBaseUrl } } } } }
    )
    .AddTransforms(builderContext => {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            builderContext.AddRequestTransform(async context => {
                context.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                await ValueTask.CompletedTask;
            });
        }
    });

var app = builder.Build();


if (app.Environment.IsDevelopment()) { app.UseWebAssemblyDebugging(); }
else { app.UseExceptionHandler("/Error", true); app.UseHsts(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting(); // 1. 路由

// 2. 认证和授权
app.UseAuthentication();
app.UseAuthorization();

// 3. 防伪
app.UseAntiforgery();

// 映射常规端点
app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MicroChat.Client._Imports).Assembly);

// 映射YARP代理，并为其应用一个专用的保护管道
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseMiddleware<AccessKeyMiddleware>();
});

app.Run();