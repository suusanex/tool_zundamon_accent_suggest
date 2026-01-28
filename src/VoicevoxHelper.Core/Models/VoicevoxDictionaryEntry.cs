namespace VoicevoxHelper.Core.Models;

/// <summary>
/// VOICEVOXユーザー辞書エントリ。
/// </summary>
public sealed class VoicevoxDictionaryEntry
{
    public string Surface { get; init; } = string.Empty;
    public string Pronunciation { get; init; } = string.Empty;
    public int AccentType { get; init; }
    public int Priority { get; init; } = 5;
}
