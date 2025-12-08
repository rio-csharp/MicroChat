using Microsoft.JSInterop;
using System.Text.Json;

namespace MicroChat.Client.Services;

/// <summary>
/// Service for interacting with browser IndexedDB storage in Blazor WebAssembly.
/// Provides thread-safe, high-performance database operations with automatic connection management.
/// </summary>
public sealed class IndexedDbService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        
        // Import the JavaScript module lazily with error handling
        _moduleTask = new Lazy<Task<IJSObjectReference>>(async () =>
        {
            try
            {
                return await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/indexedDb.js");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load IndexedDB JavaScript module.", ex);
            }
        });
    }

    /// <summary>
    /// Adds or updates a record in the specified store (upsert operation).
    /// </summary>
    /// <typeparam name="T">The type of record to add</typeparam>
    /// <param name="storeName">The name of the object store</param>
    /// <param name="record">The record to add or update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddRecordAsync<T>(string storeName, T record, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(storeName);
        ArgumentNullException.ThrowIfNull(record);
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("addRecord", cancellationToken, storeName, record);
    }

    /// <summary>
    /// Updates a record in the specified store. Creates the record if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The type of record to update</typeparam>
    /// <param name="storeName">The name of the object store</param>
    /// <param name="record">The record to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateRecordAsync<T>(string storeName, T record, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(storeName);
        ArgumentNullException.ThrowIfNull(record);
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("updateRecord", cancellationToken, storeName, record);
    }

    /// <summary>
    /// Retrieves a single record by its key.
    /// </summary>
    /// <typeparam name="T">The type of record to retrieve</typeparam>
    /// <param name="storeName">The name of the object store</param>
    /// <param name="key">The key of the record to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The record if found, otherwise null</returns>
    public async Task<T?> GetRecordAsync<T>(string storeName, object key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(storeName);
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        return await module.InvokeAsync<T?>("getRecord", cancellationToken, storeName, key);
    }

    /// <summary>
    /// Retrieves all records from the specified store.
    /// </summary>
    /// <typeparam name="T">The type of records to retrieve</typeparam>
    /// <param name="storeName">The name of the object store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all records in the store</returns>
    public async Task<IReadOnlyList<T>> GetRecordsAsync<T>(string storeName, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(storeName);
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        var result = await module.InvokeAsync<T[]>("getRecords", cancellationToken, storeName);
        return result ?? Array.Empty<T>();
    }

    /// <summary>
    /// Deletes a single record by its key.
    /// </summary>
    /// <param name="storeName">The name of the object store</param>
    /// <param name="key">The key of the record to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteRecordAsync(string storeName, object key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(storeName);
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("deleteRecord", cancellationToken, storeName, key);
    }

    /// <summary>
    /// Deletes all records from the specified store.
    /// </summary>
    /// <param name="storeName">The name of the object store to clear</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearStoreAsync(string storeName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(storeName);
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("clearStore", cancellationToken, storeName);
    }

    /// <summary>
    /// Gets information about the database including record counts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database information</returns>
    public async Task<DatabaseInfo?> GetDatabaseInfoAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var module = await GetModuleAsync(cancellationToken);
        return await module.InvokeAsync<DatabaseInfo?>("getDbInfo", cancellationToken);
    }

    /// <summary>
    /// Closes the IndexedDB connection. Useful for cleanup or testing scenarios.
    /// The connection will be automatically reopened on the next operation.
    /// </summary>
    public async Task CloseDbAsync()
    {
        if (_disposed || !_moduleTask.IsValueCreated)
            return;

        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("closeDb");
        }
        catch (JSDisconnectedException)
        {
            // Circuit has been disconnected, ignore
        }
        catch (Exception ex)
        {
            // Log but don't throw on cleanup
            Console.WriteLine($"Error closing database: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the JavaScript module with proper synchronization
    /// </summary>
    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _moduleTask.Value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(IndexedDbService));
    }

    /// <summary>
    /// Disposes the service and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await CloseDbAsync();

            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing IndexedDbService: {ex.Message}");
        }
        finally
        {
            _semaphore.Dispose();
        }
    }
}

/// <summary>
/// Information about an IndexedDB database
/// </summary>
public sealed class DatabaseInfo
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<StoreInfo> Stores { get; set; } = new();
}

/// <summary>
/// Information about an object store
/// </summary>
public sealed class StoreInfo
{
    public string Name { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
