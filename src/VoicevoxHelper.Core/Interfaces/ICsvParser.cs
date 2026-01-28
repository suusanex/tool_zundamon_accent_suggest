using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// CSV辞書パーサー。
/// </summary>
public interface ICsvParser
{
    IReadOnlyList<DictionaryCandidate> Read(string csvText);

    string Write(IReadOnlyList<DictionaryCandidate> candidates);
}
