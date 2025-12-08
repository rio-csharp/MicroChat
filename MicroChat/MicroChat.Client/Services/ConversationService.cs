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
        await _dbService.AddRecordAsync(_conversationStore, conversation);
        NotifyStateChanged();
    }

    public async Task UpdateConversationAsync(Conversation conversation)
    {
        var index = Conversations.FindIndex(c => c.Id == conversation.Id);
        if (index == -1)
        {
            return;
        }

        Conversations[index] = conversation;
        await _dbService.UpdateRecordAsync(_conversationStore, conversation);
        NotifyStateChanged();

    }

    public async Task DeleteConversationAsync(Guid conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            Conversations.Remove(conversation);
            // Assuming a DeleteRecord method exists in IndexedDbService
            await _dbService.DeleteRecordAsync(_conversationStore, conversationId);

            if (SelectedConversationId == conversationId)
            {
                SelectedConversationId = Conversations.FirstOrDefault()?.Id;
            }

            NotifyStateChanged();
        }
    }

    public async Task LoadConversationsAsync()
    {
        Conversations = (await _dbService.GetRecordsAsync<Conversation>(_conversationStore)).ToList();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
