using MicroChat.Client.Models;
using System.Collections.Concurrent;
using System.Text;

namespace MicroChat.Client.Services;

/// <summary>
/// 管理多个会话的并发流式传输任务
/// </summary>
public class StreamingTaskManager
{
    private readonly ChatService _chatService;
    private readonly ConcurrentDictionary<Guid, StreamingTask> _activeTasks = new();

    public event Action<Guid>? OnStreamingUpdate;
    public event Action<Guid, bool>? OnStreamingStatusChanged;

    public StreamingTaskManager(ChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// 检查指定会话是否正在进行流式传输
    /// </summary>
    public bool IsStreaming(Guid conversationId)
    {
        return _activeTasks.ContainsKey(conversationId);
    }

    /// <summary>
    /// 获取指定会话的流式内容
    /// </summary>
    public string GetStreamingContent(Guid conversationId)
    {
        if (_activeTasks.TryGetValue(conversationId, out var task))
        {
            return task.Content.ToString();
        }
        return string.Empty;
    }

    /// <summary>
    /// 开始一个新的流式传输任务
    /// </summary>
    public async Task StartStreamingAsync(
        Conversation conversation, 
        string userMessage,
        Func<string, Task>? onCompleted = null,
        Func<string, Task>? onError = null)
    {
        if (conversation.Id == Guid.Empty)
            throw new ArgumentException("Invalid conversation ID", nameof(conversation));

        // 如果该会话已经有正在进行的流式传输，先取消它
        if (_activeTasks.TryGetValue(conversation.Id, out var existingTask))
        {
            await CancelStreamingAsync(conversation.Id);
        }

        var cts = new CancellationTokenSource();
        var streamingTask = new StreamingTask
        {
            ConversationId = conversation.Id,
            CancellationTokenSource = cts,
            Content = new StringBuilder()
        };

        _activeTasks[conversation.Id] = streamingTask;
        OnStreamingStatusChanged?.Invoke(conversation.Id, true);

        // 在后台启动流式传输任务
        _ = Task.Run(async () => await ProcessStreamingAsync(
            conversation, 
            userMessage, 
            streamingTask, 
            onCompleted, 
            onError));
    }

    /// <summary>
    /// 取消指定会话的流式传输
    /// </summary>
    public async Task CancelStreamingAsync(Guid conversationId)
    {
        if (_activeTasks.TryRemove(conversationId, out var task))
        {
            task.CancellationTokenSource.Cancel();
            task.CancellationTokenSource.Dispose();
            OnStreamingStatusChanged?.Invoke(conversationId, false);
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 取消所有正在进行的流式传输
    /// </summary>
    public async Task CancelAllStreamingAsync()
    {
        var tasks = _activeTasks.Values.ToList();
        _activeTasks.Clear();

        foreach (var task in tasks)
        {
            task.CancellationTokenSource.Cancel();
            task.CancellationTokenSource.Dispose();
            OnStreamingStatusChanged?.Invoke(task.ConversationId, false);
        }

        await Task.CompletedTask;
    }

    private async Task ProcessStreamingAsync(
        Conversation conversation,
        string userMessage,
        StreamingTask streamingTask,
        Func<string, Task>? onCompleted,
        Func<string, Task>? onError)
    {
        try
        {
            await foreach (var chunk in _chatService.SendMessageStreamAsync(
                conversation,
                userMessage,
                streamingTask.CancellationTokenSource.Token))
            {
                streamingTask.Content.Append(chunk);
                conversation.StreamingContent = streamingTask.Content.ToString();
                OnStreamingUpdate?.Invoke(conversation.Id);
            }

            // 流式传输完成
            var finalContent = streamingTask.Content.ToString();
            _activeTasks.TryRemove(conversation.Id, out _);
            OnStreamingStatusChanged?.Invoke(conversation.Id, false);

            if (onCompleted != null)
            {
                await onCompleted(finalContent);
            }
        }
        catch (OperationCanceledException)
        {
            // 用户主动取消，不需要特殊处理
            _activeTasks.TryRemove(conversation.Id, out _);
            OnStreamingStatusChanged?.Invoke(conversation.Id, false);
        }
        catch (Exception ex)
        {
            // 发生错误
            _activeTasks.TryRemove(conversation.Id, out _);
            OnStreamingStatusChanged?.Invoke(conversation.Id, false);

            if (onError != null)
            {
                await onError(ex.Message);
            }
        }
        finally
        {
            streamingTask.CancellationTokenSource.Dispose();
        }
    }

    private class StreamingTask
    {
        public Guid ConversationId { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = null!;
        public StringBuilder Content { get; set; } = new();
    }
}
