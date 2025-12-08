using System.Net.Http.Json;

namespace MicroChat.Client.Services;

public class ModelService
{
    private readonly HttpClient _http;

    public ModelService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<string>> GetModelsAsync()
    {
        return await _http.GetFromJsonAsync<List<string>>("api/models") ?? new List<string>();
    }
}
