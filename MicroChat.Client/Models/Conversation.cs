using System.Text.Json.Serialization;

namespace MicroChat.Client.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public AIModel? AIModel { get; set; }
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// 当前流式传输的临时消息（不持久化到数据库）
    /// </summary>
    [JsonIgnore]
    public Message? StreamingMessage { get; set; }

    /// <summary>
    /// 当前流式传输的内容（不持久化到数据库）
    /// </summary>
    [JsonIgnore]
    public string StreamingContent { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在进行流式传输（不持久化到数据库）
    /// </summary>
    [JsonIgnore]
    public bool IsStreaming { get; set; }

    // Todo: Add configurations like temperature, max tokens, etc.
}
