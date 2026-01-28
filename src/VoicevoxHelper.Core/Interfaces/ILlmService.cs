using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// LLM連携サービス。
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// 辞書候補を抽出する。
    /// </summary>
    Task<IReadOnlyList<DictionaryCandidate>> ExtractDictionaryCandidatesAsync(
        Script script,
        CancellationToken cancellationToken);

    /// <summary>
    /// 台本をリライトする。
    /// </summary>
    Task<string> RewriteScriptAsync(Script script, CancellationToken cancellationToken);
}
