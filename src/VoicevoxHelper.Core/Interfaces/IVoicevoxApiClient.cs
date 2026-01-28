using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// VOICEVOXユーザー辞書APIクライアント。
/// </summary>
public interface IVoicevoxApiClient
{
    Task<IReadOnlyDictionary<string, VoicevoxDictionaryEntry>> GetUserDictionaryAsync(CancellationToken cancellationToken);

    Task<string> CreateWordAsync(VoicevoxDictionaryEntry entry, CancellationToken cancellationToken);

    Task UpdateWordAsync(string uuid, VoicevoxDictionaryEntry entry, CancellationToken cancellationToken);

    Task DeleteWordAsync(string uuid, CancellationToken cancellationToken);
}
