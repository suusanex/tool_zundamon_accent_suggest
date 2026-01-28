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
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, VoicevoxDictionaryEntry>>(
                "/user_dict",
                cancellationToken);
            return response ?? new Dictionary<string, VoicevoxDictionaryEntry>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Get user dictionary timeout: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIのタイムアウトが発生しました。", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Get user dictionary connection failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIへの接続に失敗しました。", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Get user dictionary failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("ユーザー辞書の取得に失敗しました。", ex);
        }
    }

    public async Task<string> CreateWordAsync(VoicevoxDictionaryEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/user_dict_word", entry, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("uuid").GetString() ?? string.Empty;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Create word timeout: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIのタイムアウトが発生しました。", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Create word connection failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIへの接続に失敗しました。", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Create word failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("辞書登録に失敗しました。", ex);
        }
    }

    public async Task UpdateWordAsync(string uuid, VoicevoxDictionaryEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/user_dict_word/{uuid}", entry, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Update word timeout: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIのタイムアウトが発生しました。", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Update word connection failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIへの接続に失敗しました。", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Update word failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("辞書更新に失敗しました。", ex);
        }
    }

    public async Task DeleteWordAsync(string uuid, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/user_dict_word/{uuid}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Delete word timeout: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIのタイムアウトが発生しました。", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Delete word connection failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("VOICEVOX APIへの接続に失敗しました。", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Delete word failed: {Exception}", ex.ToString());
            throw new VoicevoxApiException("辞書削除に失敗しました。", ex);
        }
    }
}
