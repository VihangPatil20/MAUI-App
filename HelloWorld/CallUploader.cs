using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace HelloWorld;

public static class CallUploader
{
    public static async Task TryUploadAsync(CallRecord record)
    {
        var apiUrl = await SecureStorage.Default.GetAsync("CallsApiUrl");
        var apiKey = await SecureStorage.Default.GetAsync("CallsApiKey");

        // Cloud upload stays disabled until the user configures it.
        if (string.IsNullOrWhiteSpace(apiUrl) ||
            string.IsNullOrWhiteSpace(apiKey))
            return;

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await client.PostAsJsonAsync(
            $"{apiUrl.TrimEnd('/')}/calls", record);

        response.EnsureSuccessStatusCode();
    }
}