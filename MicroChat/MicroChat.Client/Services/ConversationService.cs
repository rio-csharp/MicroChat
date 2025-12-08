using MicroChat.Client.Models;

namespace MicroChat.Client.Services;

public class ConversationService(IndexedDbService dbService, StreamingTaskManager streamingTaskManager)
{
    public event Action? OnChange;
    public List<Conversation> Conversations { get; set; } = new();

    private Guid? _selectedConversationId;
    public Guid? SelectedConversationId
    {
        get => _selectedConversationId;
        set
        {
            if (value != _selectedConversationId)
            {
                _selectedConversationId = value;
                NotifyStateChanged();
            }
        }
    }
    public Conversation? SelectedConversation => Conversations.FirstOrDefault(c => c.Id == SelectedConversationId);
    private readonly IndexedDbService _dbService = dbService;
    private readonly StreamingTaskManager _streamingTaskManager = streamingTaskManager;
    private readonly string _conversationStore = "conversations";

    public async Task AddConversationAsync(Conversation conversation)
    {
        Conversations.Add(conversation);
        SelectedConversationId = conversation.Id;
        await _dbService.OpenDbAsync();
        await _dbService.AddRecordAsync(_conversationStore, conversation);
        NotifyStateChanged();
    }

    public async Task UpdateConversationAsync(Conversation conversation)
    {
        var index = Conversations.FindIndex(c => c.Id == conversation.Id);
        if (index != -1)
        {
            Conversations[index] = conversation;
            await _dbService.OpenDbAsync();
            await _dbService.UpdateRecordAsync(_conversationStore, conversation);
            NotifyStateChanged();
        }
    }

    public async Task LoadConversationsAsync()
    {
        await _dbService.OpenDbAsync();
        Conversations = (await _dbService.GetRecordsAsync<Conversation>(_conversationStore)).ToList();
        NotifyStateChanged();
    }

    public async Task DeleteConversationAsync(Guid conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            // 取消该会话正在进行的流式传输
            await _streamingTaskManager.CancelStreamingAsync(conversationId);
            
            Conversations.Remove(conversation);
            await _dbService.OpenDbAsync();
            // Assuming a DeleteRecord method exists in IndexedDbService
            await _dbService.DeleteRecordAsync(_conversationStore, conversationId);
            if (SelectedConversationId == conversationId)
            {
                SelectedConversationId = Conversations.FirstOrDefault()?.Id;
            }
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 为指定会话发送消息并启动流式传输
    /// </summary>
    public async Task SendMessageAsync(Guid conversationId, string userMessage)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation == null || string.IsNullOrWhiteSpace(userMessage))
            return;

        // 添加用户消息
        var userMsg = new Message
        {
            Sender = MessageRole.User,
            Content = userMessage,
            Time = DateTime.Now,
            Model = conversation.AIModel
        };
        conversation.Messages.Add(userMsg);
        await UpdateConversationAsync(conversation);

        // 初始化流式传输状态
        conversation.StreamingMessage = new Message
        {
            Sender = MessageRole.Assistant,
            Time = DateTime.Now,
            Content = string.Empty
        };
        conversation.StreamingContent = string.Empty;
        conversation.IsStreaming = true;
        NotifyStateChanged();

        // 启动流式传输
        await _streamingTaskManager.StartStreamingAsync(
            conversation,
            userMessage,
            onCompleted: async (content) => await OnStreamingCompletedAsync(conversationId, content),
            onError: async (error) => await OnStreamingErrorAsync(conversationId, error)
        );
    }

    /// <summary>
    /// 流式传输更新回调
    /// </summary>
    private void OnStreamingUpdate(Guid conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            conversation.StreamingContent = _streamingTaskManager.GetStreamingContent(conversationId);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 流式传输完成回调
    /// </summary>
    private async Task OnStreamingCompletedAsync(Guid conversationId, string content)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
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
            
            await UpdateConversationAsync(conversation);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 流式传输错误回调
    /// </summary>
    private async Task OnStreamingErrorAsync(Guid conversationId, string error)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
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
            
            await UpdateConversationAsync(conversation);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 初始化流式传输管理器的事件
    /// </summary>
    public void InitializeStreamingEvents()
    {
        _streamingTaskManager.OnStreamingUpdate += OnStreamingUpdate;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
