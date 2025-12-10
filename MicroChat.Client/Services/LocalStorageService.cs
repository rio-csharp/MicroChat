using Microsoft.JSInterop;
using System.Text;

namespace MicroChat.Client.Services;

/// <summary>
/// Service for managing access keys and other settings in browser localStorage
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private const string AccessKeyStorageKey = "microchat_access_key";

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Saves the access key to localStorage with Base64 encoding
    /// </summary>
    public async Task SetAccessKeyAsync(string accessKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            throw new ArgumentException("Access key cannot be null or empty", nameof(accessKey));
        }

        // Encode to Base64 for basic obfuscation
        var encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessKey));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessKeyStorageKey, encodedKey);
    }

    /// <summary>
    /// Retrieves the access key from localStorage and decodes it
    /// </summary>
    public async Task<string?> GetAccessKeyAsync()
    {
        try
        {
            var encodedKey = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessKeyStorageKey);

            if (string.IsNullOrEmpty(encodedKey))
            {
                return null;
            }

            // Decode from Base64
            var decodedBytes = Convert.FromBase64String(encodedKey);
            return Encoding.UTF8.GetString(decodedBytes);
        }
        catch
        {
            // If decoding fails, return null
            return null;
        }
    }

    /// <summary>
    /// Removes the access key from localStorage
    /// </summary>
    public async Task RemoveAccessKeyAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessKeyStorageKey);
    }

    /// <summary>
    /// Checks if an access key exists in localStorage
    /// </summary>
    public async Task<bool> HasAccessKeyAsync()
    {
        var key = await GetAccessKeyAsync();
        return !string.IsNullOrEmpty(key);
    }
}
