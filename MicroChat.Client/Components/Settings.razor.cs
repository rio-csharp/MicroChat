using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components;

namespace MicroChat.Client;

public partial class Settings
{
    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private LocalStorageService LocalStorageService { get; set; } = default!;

    private string AccessKey { get; set; } = string.Empty;
    private bool ShowAccessKey { get; set; } = false;
    private string StatusMessage { get; set; } = string.Empty;
    private string StatusClass { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // 加载已保存的 AccessKey
        var savedKey = await LocalStorageService.GetAccessKeyAsync();
        if (!string.IsNullOrEmpty(savedKey))
        {
            AccessKey = savedKey;
        }
    }

    private void ToggleAccessKeyVisibility()
    {
        ShowAccessKey = !ShowAccessKey;
    }

    private async Task SaveSettings()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AccessKey))
            {
                StatusMessage = "Access Key 不能为空";
                StatusClass = "error";
                return;
            }

            await LocalStorageService.SetAccessKeyAsync(AccessKey);

            StatusMessage = "设置已保存";
            StatusClass = "success";

            // 延迟关闭，让用户看到成功消息
            await Task.Delay(1000);
            await OnClose.InvokeAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            StatusClass = "error";
        }
    }

    private async Task HandleOverlayClick()
    {
        await OnClose.InvokeAsync();
    }
}
