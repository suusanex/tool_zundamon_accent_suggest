using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoicevoxHelper.Core.Exceptions;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.VoicevoxApi;

/// <summary>
/// VOICEVOXユーザー辞書APIクライアント。
/// </summary>
public sealed class VoicevoxApiClient : IVoicevoxApiClient
{
    private readonly HttpClient _httpClient;
    private readonly VoicevoxSettings _settings;
    private readonly ILogger<VoicevoxApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VoicevoxApiClient(
        HttpClient httpClient,
        IOptions<VoicevoxSettings> options,
        ILogger<VoicevoxApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public async Task<IReadOnlyDictionary<string, VoicevoxDictionaryEntry>> GetUserDictionaryAsync(
        CancellationToken cancellationToken)
    {
        const string operation = "GetUserDictionary";
        const string path = "/user_dict";
        LogVoicevoxRequest(operation, HttpMethod.Get, path);

        var response = await _httpClient.GetAsync(path, cancellationToken);
        var body = await ReadAndLogResponseAsync(operation, response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new VoicevoxApiException(
                "ユーザー辞書の取得に失敗しました。",
                new HttpRequestException($"VOICEVOX {(int)response.StatusCode}: {Summarize(body, 512)}"));
        }

        var dictionary = JsonSerializer.Deserialize<Dictionary<string, VoicevoxDictionaryEntry>>(body, _jsonOptions);
        return dictionary ?? new Dictionary<string, VoicevoxDictionaryEntry>();
    }

    public async Task<string> CreateWordAsync(VoicevoxDictionaryEntry entry, CancellationToken cancellationToken)
    {
        const string operation = "CreateWord";
        const string path = "/user_dict_word";
        LogVoicevoxRequest(operation, HttpMethod.Post, path, FormatEntry(entry));

        var response = await _httpClient.PostAsJsonAsync(path, entry, cancellationToken);
        var body = await ReadAndLogResponseAsync(operation, response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new VoicevoxApiException(
                "辞書登録に失敗しました。",
                new HttpRequestException($"VOICEVOX {(int)response.StatusCode}: {Summarize(body, 512)}"));
        }

        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("uuid").GetString() ?? string.Empty;
    }

    public async Task UpdateWordAsync(string uuid, VoicevoxDictionaryEntry entry, CancellationToken cancellationToken)
    {
        const string operation = "UpdateWord";
        var path = $"/user_dict_word/{uuid}";
        LogVoicevoxRequest(operation, HttpMethod.Put, path, FormatEntry(entry));

        var response = await _httpClient.PutAsJsonAsync(path, entry, cancellationToken);
        var body = await ReadAndLogResponseAsync(operation, response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new VoicevoxApiException(
                "辞書更新に失敗しました。",
                new HttpRequestException($"VOICEVOX {(int)response.StatusCode}: {Summarize(body, 512)}"));
        }
    }

    public async Task DeleteWordAsync(string uuid, CancellationToken cancellationToken)
    {
        const string operation = "DeleteWord";
        var path = $"/user_dict_word/{uuid}";
        LogVoicevoxRequest(operation, HttpMethod.Delete, path, $"uuid={uuid}");

        var response = await _httpClient.DeleteAsync(path, cancellationToken);
        var body = await ReadAndLogResponseAsync(operation, response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new VoicevoxApiException(
                "辞書削除に失敗しました。",
                new HttpRequestException($"VOICEVOX {(int)response.StatusCode}: {Summarize(body, 512)}"));
        }
    }

    private void LogVoicevoxRequest(string operation, HttpMethod method, string path, string? detail = null)
    {
        if (string.IsNullOrEmpty(detail))
        {
            _logger.LogInformation("{Operation} request {Method} {Path}", operation, method.Method, path);
        }
        else
        {
            _logger.LogInformation("{Operation} request {Method} {Path} detail {Detail}", operation, method.Method, path, detail);
        }
    }

    private string FormatEntry(VoicevoxDictionaryEntry entry)
    {
        return $"surface={entry.Surface}, pronunciation={entry.Pronunciation}, accentType={entry.AccentType}";
    }

    private static string Summarize(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private async Task<string> ReadAndLogResponseAsync(string operation, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var snippet = Summarize(body);
        var method = response.RequestMessage?.Method?.Method ?? "UNKNOWN";
        var uri = response.RequestMessage?.RequestUri?.ToString() ?? "UNKNOWN";
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "{Operation} {Method} {Uri} response {StatusCode} body {Body}",
                operation,
                method,
                uri,
                (int)response.StatusCode,
                snippet);
        }
        else
        {
            _logger.LogError(
                "{Operation} {Method} {Uri} response {StatusCode} body {Body}",
                operation,
                method,
                uri,
                (int)response.StatusCode,
                snippet);
        }

        return body;
    }
}
