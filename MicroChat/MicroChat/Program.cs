using MicroChat.Components;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddControllers();

// 从配置读取 API 信息
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiKey = builder.Configuration["ApiKey"];

// 配置 YARP 反向代理（代码配置方式）
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        new[]
        {
            new RouteConfig
            {
                RouteId = "api-proxy",
                ClusterId = "api-cluster",
                Match = new RouteMatch
                {
                    Path = "/api/proxy/{**catch-all}"
                },
                Transforms = new List<IReadOnlyDictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "PathPattern", "{**catch-all}" }
                    }
                }
            }
        },
        new[]
        {
            new ClusterConfig
            {
                ClusterId = "api-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        "destination1",
                        new DestinationConfig
                        {
                            Address = string.IsNullOrWhiteSpace(apiBaseUrl) ? "https://api.openai.com" : apiBaseUrl
                        }
                    }
                }
            }
        })
    .AddTransforms(builderContext =>
    {
        // 添加 API Key 到请求头
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            builderContext.AddRequestTransform(async context =>
            {
                context.ProxyRequest.Headers.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                await ValueTask.CompletedTask;
            });
        }
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapReverseProxy();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MicroChat.Client._Imports).Assembly);

app.Run();
