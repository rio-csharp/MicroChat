using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MicroChat.Client.Pages;

public partial class ChatPanel : IDisposable
{
    [Inject]
    private ConversationService ConversationService { get; set; } = default!;

    [Inject]
    private StreamingTaskManager StreamingTaskManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<ChatPanel>? _dotNetHelper;
    private bool _isAtBottom = true;
    private bool _isAtTop = true;
    private Guid? _lastRenderedConversationId;

    protected override void OnInitialized()
    {
        ConversationService.OnChange += OnStateChanged;
        StreamingTaskManager.OnStreamingUpdate += OnStreamingUpdate;
    }

    private void OnStreamingUpdate(Guid conversationId)
    {
        // 只处理当前会话的流式更新
        if (ConversationService.SelectedConversation?.Id == conversationId)
        {
            // 异步滚动到底部
            _ = InvokeAsync(async () => await ScrollToBottomInstantAsync());
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && ConversationService.SelectedConversation != null)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Pages/ChatPanel.razor.js");
            _dotNetHelper = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("init", _dotNetHelper);
            await ScrollToBottomInstantAsync();
            _lastRenderedConversationId = ConversationService.SelectedConversation.Id;
        }
        else if (ConversationService.SelectedConversation != null &&
                 _lastRenderedConversationId != ConversationService.SelectedConversation.Id)
        {
            // 切换了会话，立即滚动到底部（无动画）
            await ScrollToBottomInstantAsync();
            _lastRenderedConversationId = ConversationService.SelectedConversation.Id;
        }
        else if (ConversationService.SelectedConversation?.IsStreaming == true)
        {
            // 流式传输时立即滚动到底部（无动画）
            await ScrollToBottomInstantAsync();
        }
    }

    private void OnStateChanged()
    {
        StateHasChanged();
    }

    [JSInvokable]
    public void UpdateButtonState(bool isAtBottom, bool isAtTop)
    {
        if (_isAtBottom != isAtBottom || _isAtTop != isAtTop)
        {
            _isAtBottom = isAtBottom;
            _isAtTop = isAtTop;
            StateHasChanged();
        }
    }

    private async Task ScrollToBottomAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("scrollToBottom");
        }
    }

    private async Task ScrollToBottomInstantAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("scrollToBottomInstant");
        }
    }

    private async Task ScrollToTopAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("scrollToTop");
        }
    }

    public void Dispose()
    {
        ConversationService.OnChange -= OnStateChanged;
        StreamingTaskManager.OnStreamingUpdate -= OnStreamingUpdate;
        _dotNetHelper?.Dispose();
        _jsModule?.DisposeAsync();
    }
}
