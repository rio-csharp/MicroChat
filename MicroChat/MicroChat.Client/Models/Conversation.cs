namespace MicroChat.Client.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public AIModel? AIModel { get; set; }
    public List<Message> Messages { get; set; } = new();
}
