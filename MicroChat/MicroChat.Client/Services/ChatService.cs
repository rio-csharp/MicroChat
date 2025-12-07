using MicroChat.Client.Models;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroChat.Client.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        Conversation conversation,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = conversation.Messages
            .Select(m => new ChatMessage
            {
                Role = m.Sender == MessageRole.User ? "user" : "assistant",
                Content = m.Content
            })
            .ToList();

        // 添加当前用户消息
        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = userMessage
        });

        var request = new ChatCompletionRequest
        {
            Model = conversation.AIModel?.Id ?? "gpt-4",
            Messages = messages,
            Stream = true
        };

        var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/proxy/v1/chat/completions")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"API returned {response.StatusCode}: {errorContent}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("data: ") || line.Length < 7)
                continue;

            var data = line.Substring(6).Trim();

            if (data == "[DONE]")
                break;

            ChatCompletionStreamResponse? streamResponse = null;
            try
            {
                streamResponse = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (JsonException)
            {
                // 跳过无法解析的行
                continue;
            }

            if (streamResponse?.Choices != null && streamResponse.Choices.Count > 0)
            {
                var content = streamResponse.Choices[0]?.Delta?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }
        }
    }
}

// DTO 类
public class ChatCompletionRequest
{
    public string Model { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public bool Stream { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatCompletionStreamResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<StreamChoice>? Choices { get; set; }
}

public class StreamChoice
{
    public int Index { get; set; }
    public StreamDelta? Delta { get; set; }
    public string? FinishReason { get; set; }
}

public class StreamDelta
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}
