using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<StreamingTaskManager>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<ModelService>();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();
await host.RunAsync();
