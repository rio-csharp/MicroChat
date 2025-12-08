using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MicroChat.Client.Pages;

public partial class Home
{
    [Inject]
    private ConversationService ConversationService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private bool ShowMobileContent = false;
    private bool IsContainerMode = true; // 默认为容器模式

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 从 localStorage 加载容器模式设置
            var storedValue = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "containerMode");
            if (!string.IsNullOrEmpty(storedValue) && bool.TryParse(storedValue, out var isContainer))
            {
                IsContainerMode = isContainer;
                StateHasChanged();
            }
        }
    }

    private void HandleChatSelected()
    {
        ShowMobileContent = true;
    }

    private void HandleBackToList()
    {
        ShowMobileContent = false;
    }

    private async Task ToggleContainerMode()
    {
        IsContainerMode = !IsContainerMode;
        // 保存到 localStorage
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", "containerMode", IsContainerMode.ToString());
    }
}
