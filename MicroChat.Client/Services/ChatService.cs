using MicroChat.Client.Models;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroChat.Client.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly LocalStorageService _localStorageService;

    public ChatService(HttpClient httpClient, LocalStorageService localStorageService)
    {
        _httpClient = httpClient;
        _localStorageService = localStorageService;
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        Conversation conversation,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateConversation(conversation);

        var messages = BuildChatMessages(conversation, userMessage);
        var request = CreateChatCompletionRequest(conversation.AIModel!.Id!, messages);

        using var response = await SendHttpRequestAsync(request, cancellationToken);

        await foreach (var content in ProcessStreamResponseAsync(response, cancellationToken))
        {
            yield return content;
        }
    }

    private static void ValidateConversation(Conversation conversation)
    {
        if (conversation.AIModel == null || string.IsNullOrWhiteSpace(conversation.AIModel.Id))
        {
            throw new InvalidOperationException("Conversation does not have a valid AI model assigned.");
        }
    }

    private static List<ChatMessage> BuildChatMessages(Conversation conversation, string userMessage)
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

        return messages;
    }

    private static ChatCompletionRequest CreateChatCompletionRequest(string modelId, List<ChatMessage> messages)
    {
        return new ChatCompletionRequest
        {
            Model = modelId,
            Messages = messages,
            Stream = true
        };
    }

    private async Task<HttpResponseMessage> SendHttpRequestAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/proxy/v1/chat/completions")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        // Add AccessKey header if available
        var accessKey = await _localStorageService.GetAccessKeyAsync();
        if (!string.IsNullOrEmpty(accessKey))
        {
            httpRequest.Headers.Add("X-Access-Key", accessKey);
        }

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"API returned {response.StatusCode}: {errorContent}");
        }

        return response;
    }

    private static async IAsyncEnumerable<string> ProcessStreamResponseAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ") || line.Length < 7)
                continue;

            var data = line.Substring(6).Trim();

            if (data == "[DONE]")
                break;

            var content = TryParseStreamContent(data);
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    private static string? TryParseStreamContent(string data)
    {
        try
        {
            var streamResponse = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return streamResponse?.Choices?.FirstOrDefault()?.Delta?.Content;
        }
        catch (JsonException)
        {
            // 跳过无法解析的行
            return null;
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
