using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

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
