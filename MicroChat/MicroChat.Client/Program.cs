using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<StreamingTaskManager>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<ModelService>();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();

// 初始化 ConversationService 的流事件
var conversationService = host.Services.GetRequiredService<ConversationService>();
conversationService.InitializeStreamingEvents();

await host.RunAsync();
