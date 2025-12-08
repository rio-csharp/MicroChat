namespace MicroChat.Client.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageRole Sender { get; set; }
    public AIModel? Model { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Time { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System
}