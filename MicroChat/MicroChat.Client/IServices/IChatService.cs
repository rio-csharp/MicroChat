using MicroChat.Client.Models;

namespace MicroChat.Client.IServices;

public interface IChatService
{
    Task<List<Conversation>> GetConversationsAsync();
    Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
    Task AddConversation(Conversation conversation);
}
