using MicroChat.Client.Models;

namespace MicroChat.Client.Services;

public class ConversationService(IndexedDbService dbService)
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

    public async Task<Conversation> CreateNewConversationAsync(AIModel? aiModel = null)
    {
        var newConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = "New Chat",
            AIModel = aiModel,
            Messages = new List<Message>()
            {
                new Message
                {
                    Id = Guid.NewGuid(),
                    Content = "Hello! How can I assist you today?",
                    Time = DateTime.Now,
                    Sender = MessageRole.System
                }
            }
        };
        await AddConversationAsync(newConversation);
        return newConversation;
    }

    public async Task LoadConversationsAsync()
    {
        Conversations = (await _dbService.GetRecordsAsync<Conversation>(_conversationStore)).ToList();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
