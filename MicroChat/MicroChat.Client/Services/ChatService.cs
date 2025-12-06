using MicroChat.Client.IServices;
using MicroChat.Client.Models;

namespace MicroChat.Client.Services;

public class ChatService : IChatService
{
    private readonly IndexedDbService _dbService;
    private readonly string _conversationStore = "conversations";

    public ChatService(IndexedDbService dbService)
    {
        _dbService = dbService;
    }

    public async Task AddConversation(Conversation conversation)
    {
        await _dbService.OpenDb();
        await _dbService.AddRecord(_conversationStore, conversation);
    }

    public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
    {
        await _dbService.OpenDb();
        return await _dbService.GetRecord<Conversation>(_conversationStore, conversationId);
    }

    public async Task<List<Conversation>> GetConversationsAsync()
    {
        await _dbService.OpenDb();
        return (await _dbService.GetRecords<Conversation>(_conversationStore)).ToList();
    }
}
