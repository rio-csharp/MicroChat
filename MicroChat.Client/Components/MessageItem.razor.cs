using MicroChat.Client.Models;
using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MicroChat.Client.Components;

public partial class MessageItem : IAsyncDisposable
{
    [Parameter, EditorRequired]
    public Message Message { get; set; } = default!;

    [Parameter]
    public string? Content { get; set; }

    [Parameter]
    public bool IsStreaming { get; set; } = false;

    [Parameter]
    public bool IsLoading { get; set; } = false;

    [Inject]
    private MarkdownService MarkdownService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ConversationService ConversationService { get; set; } = default!;

    [Inject]
    private StreamingTaskManager StreamingTaskManager { get; set; } = default!;

    private string RenderedContent { get; set; } = string.Empty;
    private ElementReference _messageElement;
    private IJSObjectReference? _codeBlockModule;
    private bool _isSubscribedToStreaming = false;
    private bool _internalIsLoading = false;

    protected override void OnParametersSet()
    {
        // 如果没有提供自定义内容，使用消息的内容
        Content ??= Message.Content;

        // 如果是流式传输消息，订阅更新事件
        if (IsStreaming && !_isSubscribedToStreaming)
        {
            StreamingTaskManager.OnStreamingUpdate += OnStreamingContentUpdate;
            _isSubscribedToStreaming = true;

            // 立即获取当前流式内容
            if (ConversationService.SelectedConversation?.Id != null)
            {
                var currentContent = StreamingTaskManager.GetStreamingContent(ConversationService.SelectedConversation.Id);
                if (!string.IsNullOrEmpty(currentContent))
                {
                    Content = currentContent;
                    _internalIsLoading = false;
                }
                else
                {
                    _internalIsLoading = true;
                }
            }
        }
        else if (!IsStreaming && _isSubscribedToStreaming)
        {
            StreamingTaskManager.OnStreamingUpdate -= OnStreamingContentUpdate;
            _isSubscribedToStreaming = false;
        }

        // 渲染 Markdown（仅对 AI 消息且不在 loading 状态）
        if (Message.Sender != MessageRole.User && !_internalIsLoading)
        {
            RenderedContent = MarkdownService.ToHtml(Content ?? string.Empty);
        }
    }

    private void OnStreamingContentUpdate(Guid conversationId)
    {
        // 只处理当前会话的更新
        if (ConversationService.SelectedConversation?.Id == conversationId && IsStreaming)
        {
            var latestContent = StreamingTaskManager.GetStreamingContent(conversationId);

            if (!string.IsNullOrEmpty(latestContent))
            {
                Content = latestContent;
                _internalIsLoading = false;
                RenderedContent = MarkdownService.ToHtml(Content);

                InvokeAsync(StateHasChanged);
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 仅对 AI 消息初始化代码块
        if (Message.Sender != MessageRole.User && !IsStreaming)
        {
            try
            {
                _codeBlockModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/codeBlock.js");

                await _codeBlockModule.InvokeVoidAsync("initializeCodeBlocks", _messageElement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing code blocks: {ex.Message}");
            }
        }
    }

    private async Task CopyMessageAsync()
    {
        try
        {
            var contentToCopy = Content ?? Message.Content;
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", contentToCopy);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying to clipboard: {ex.Message}");
        }
    }

    private async Task DeleteMessageAsync()
    {
        if (ConversationService.SelectedConversation != null)
        {
            var conversation = ConversationService.SelectedConversation;
            var message = conversation.Messages.FirstOrDefault(m => m.Id == Message.Id);
            if (message != null)
            {
                conversation.Messages.Remove(message);
            }
            await ConversationService.UpdateConversationAsync(conversation);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isSubscribedToStreaming)
        {
            StreamingTaskManager.OnStreamingUpdate -= OnStreamingContentUpdate;
            _isSubscribedToStreaming = false;
        }

        if (_codeBlockModule is not null)
        {
            try
            {
                await _codeBlockModule.InvokeVoidAsync("cleanupCodeBlocks", _messageElement);
                await _codeBlockModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // 忽略 JS 断开连接的异常
            }
        }
    }
}
