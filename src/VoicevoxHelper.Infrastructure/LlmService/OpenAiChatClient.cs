using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.LlmService;

/// <summary>
/// Azure OpenAIのチャット補完クライアント。
/// </summary>
public sealed class OpenAiChatClient : IChatClient
{
    private readonly LlmSettings _settings;
    private readonly HttpClient _httpClient;

    public OpenAiChatClient(IOptions<LlmSettings> options)
    {
        _settings = options.Value;
        _httpClient = new HttpClient();
    }

    public async Task<ChatCompletionResult> GetChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var endpoint = _settings.Endpoint.TrimEnd('/');
        var requestUri = $"{endpoint}/openai/deployments/{_settings.Deployment}/chat/completions?api-version=2024-02-15-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("api-key", _settings.ApiKey);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        var usage = doc.RootElement.TryGetProperty("usage", out var usageElement) ? usageElement : default;

        return new ChatCompletionResult
        {
            Content = content,
            PromptTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("prompt_tokens", out var prompt) ? prompt.GetInt32() : 0,
            CompletionTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("completion_tokens", out var completion) ? completion.GetInt32() : 0,
            TotalTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("total_tokens", out var total) ? total.GetInt32() : 0
        };
    }
}
