namespace MicroChat.Client.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageRole Sender { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Time = DateTime.Now;
}

public enum MessageRole
{
    User,
    Assistant,
    System
}