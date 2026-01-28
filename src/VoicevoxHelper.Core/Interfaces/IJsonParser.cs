using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// JSON辞書パーサー。
/// </summary>
public interface IJsonParser
{
    IReadOnlyList<DictionaryCandidate> Read(string jsonText);

    string Write(IReadOnlyList<DictionaryCandidate> candidates);
}
