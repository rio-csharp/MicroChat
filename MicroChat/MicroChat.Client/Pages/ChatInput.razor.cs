using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MicroChat.Client.Services;
using MicroChat.Client.Models;

namespace MicroChat.Client.Pages;

public partial class ChatInput
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ConversationService ConversationService { get; set; } = default!;

    [Inject]
    private StreamingTaskManager StreamingTaskManager { get; set; } = default!;

    private ElementReference _textArea;
    private string _inputMessage = string.Empty;

    [Parameter]
    public bool IsStreaming { get; set; }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !IsStreaming && !string.IsNullOrWhiteSpace(_inputMessage))
        {
            await SendMessage();
        }
    }

    private async Task SendMessage()
    {
        var conversation = ConversationService.SelectedConversation;
        if (string.IsNullOrWhiteSpace(_inputMessage) || conversation == null)
            return;

        var userMessage = _inputMessage.Trim();
        _inputMessage = string.Empty;
        await ResetTextAreaHeight();

        await AddUserMessage(conversation, userMessage);
        await InitializeStreamingState(conversation);
        await StartStreaming(conversation, userMessage);
    }

    private async Task AddUserMessage(Conversation conversation, string userMessage)
    {
        var userMsg = new Message
        {
            Sender = MessageRole.User,
            Content = userMessage,
            Time = DateTime.Now,
            Model = conversation.AIModel
        };
        conversation.Messages.Add(userMsg);
        await ConversationService.UpdateConversationAsync(conversation);
    }

    private async Task InitializeStreamingState(Conversation conversation)
    {
        conversation.StreamingMessage = new Message
        {
            Sender = MessageRole.Assistant,
            Time = DateTime.Now,
            Content = string.Empty,
            Model = conversation.AIModel
        };
        conversation.StreamingContent = string.Empty;
        conversation.IsStreaming = true;
        
        // 触发状态更新，让ChatPanel显示“正在思考中”
        await ConversationService.UpdateConversationAsync(conversation);
    }

    private async Task StartStreaming(Conversation conversation, string userMessage)
    {
        await StreamingTaskManager.StartStreamingAsync(
            conversation,
            userMessage,
            onCompleted: async (content) => await OnStreamingCompleted(conversation.Id, content),
            onError: async (error) => await OnStreamingError(conversation.Id, error)
        );
    }

    /// <summary>
    /// 流式传输完成回调
    /// </summary>
    private async Task OnStreamingCompleted(Guid conversationId, string content)
    {
        var conversation = ConversationService.Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null && !string.IsNullOrEmpty(content))
        {
            var assistantMsg = new Message
            {
                Sender = MessageRole.Assistant,
                Content = content,
                Time = DateTime.Now,
                Model = conversation.AIModel
            };
            conversation.Messages.Add(assistantMsg);
            conversation.IsStreaming = false;
            conversation.StreamingContent = string.Empty;
            conversation.StreamingMessage = null;

            await ConversationService.UpdateConversationAsync(conversation);
        }
    }

    /// <summary>
    /// 流式传输错误回调
    /// </summary>
    private async Task OnStreamingError(Guid conversationId, string error)
    {
        var conversation = ConversationService.Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            var errorMsg = new Message
            {
                Sender = MessageRole.Assistant,
                Content = $"发送失败: {error}",
                Time = DateTime.Now,
                Model = conversation.AIModel
            };
            conversation.Messages.Add(errorMsg);
            conversation.IsStreaming = false;
            conversation.StreamingContent = string.Empty;
            conversation.StreamingMessage = null;

            await ConversationService.UpdateConversationAsync(conversation);
        }
    }

    private async Task AdjustTextAreaHeight()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
                const textarea = document.querySelector('.chat-input-area textarea');
                if (textarea) {
                    textarea.style.height = 'auto';
                    const scrollHeight = textarea.scrollHeight;
                    if (scrollHeight > 200) {
                        textarea.style.height = '200px';
                        textarea.classList.add('scrollable');
                    } else {
                        textarea.style.height = scrollHeight + 'px';
                        textarea.classList.remove('scrollable');
                    }
                }
            ");
        }
        catch { }
    }

    private async Task ResetTextAreaHeight()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
                const textarea = document.querySelector('.chat-input-area textarea');
                if (textarea) {
                    textarea.style.height = '36px';
                    textarea.classList.remove('scrollable');
                }
            ");
        }
        catch { }
    }
}
