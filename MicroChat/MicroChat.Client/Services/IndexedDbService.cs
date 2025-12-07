using Microsoft.JSInterop;

namespace MicroChat.Client.Services;

public class IndexedDbService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private readonly string _dbName = "MicroChatDb";
    private readonly int _version = 1;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        // Import the JavaScript module lazily.
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
           "import", "./js/indexedDb.js").AsTask());
    }

    public async Task OpenDbAsync()
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("openDb", _dbName, _version);
    }

    public async Task AddRecordAsync<T>(string storeName, T record)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("addRecord", storeName, record);
    }

    public async Task UpdateRecordAsync<T>(string storeName, T record)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("updateRecord", storeName, record);
    }

    public async Task<T?> GetRecordAsync<T>(string storeName, object key) where T : class
    {
        var module = await _moduleTask.Value;
        // Use InvokeAsync<T> to get a return value from JavaScript.
        return await module.InvokeAsync<T>("getRecord", storeName, key);
    }

    public async Task<IEnumerable<T>> GetRecordsAsync<T>(string storeNames) where T : class
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<IEnumerable<T>>("getRecords", storeNames);
    }

    public async Task DeleteRecordAsync(string storeName, object key)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("deleteRecord", storeName, key);
    }

    // Implementing IAsyncDisposable to clean up the JS module reference
    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
