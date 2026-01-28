using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.Services;

/// <summary>
/// 辞書登録サービス。
/// </summary>
public sealed class DictionaryRegistrationService : IDictionaryRegistrationService
{
    private readonly IVoicevoxApiClient _client;
    private readonly VoicevoxSettings _settings;
    private readonly ILogger<DictionaryRegistrationService> _logger;

    public DictionaryRegistrationService(
        IVoicevoxApiClient client,
        IOptions<VoicevoxSettings> options,
        ILogger<DictionaryRegistrationService> logger)
    {
        _client = client;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<ExecutionReport> RegisterAsync(
        IReadOnlyList<DictionaryCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        var failures = new List<string>();
        var success = 0;

        var existing = await _client.GetUserDictionaryAsync(cancellationToken);
        var surfaceToUuid = existing.ToDictionary(pair => pair.Value.Surface, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var entry = new VoicevoxDictionaryEntry
            {
                Surface = candidate.Surface,
                Pronunciation = candidate.Pronunciation,
                AccentType = candidate.AccentType
            };

            try
            {
                if (surfaceToUuid.TryGetValue(candidate.Surface, out var uuid))
                {
                    if (_settings.UpdateExistingWords)
                    {
                        await _client.UpdateWordAsync(uuid, entry, cancellationToken);
                        success++;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    await _client.CreateWordAsync(entry, cancellationToken);
                    success++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Dictionary registration failed: {Exception}", ex.ToString());
                failures.Add($"{candidate.Surface}: {ex.Message}");
            }
        }

        var report = new ExecutionReport
        {
            Mode = ExecutionMode.DictionaryRegistration,
            Status = failures.Count == 0 ? ExecutionStatus.Success : ExecutionStatus.Failed,
            SuccessCount = success,
            FailureCount = failures.Count,
            FailureDetails = failures,
            Duration = DateTimeOffset.UtcNow - start
        };

        _logger.LogInformation("ExecutionReport: {Report}", JsonSerializer.Serialize(report));
        return report;
    }
}
