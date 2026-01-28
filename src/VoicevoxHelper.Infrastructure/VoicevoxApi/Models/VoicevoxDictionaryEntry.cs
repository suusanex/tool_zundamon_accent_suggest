namespace VoicevoxHelper.Infrastructure.VoicevoxApi.Models;

/// <summary>
/// VOICEVOXユーザー辞書エントリDTO。
/// </summary>
public sealed class VoicevoxDictionaryEntry
{
    public string Surface { get; init; } = string.Empty;
    public string Pronunciation { get; init; } = string.Empty;
    public int AccentType { get; init; }
    public int Priority { get; init; } = 5;
}
