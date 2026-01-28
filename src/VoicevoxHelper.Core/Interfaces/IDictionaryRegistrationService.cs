using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// 辞書登録サービス。
/// </summary>
public interface IDictionaryRegistrationService
{
    Task<ExecutionReport> RegisterAsync(
        IReadOnlyList<DictionaryCandidate> candidates,
        CancellationToken cancellationToken);
}
