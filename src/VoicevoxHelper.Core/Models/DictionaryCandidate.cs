namespace VoicevoxHelper.Core.Models;

/// <summary>
/// 辞書候補を表すモデル。
/// </summary>
public sealed class DictionaryCandidate
{
    /// <summary>
    /// 単語の表記。
    /// </summary>
    public string Surface { get; init; } = string.Empty;

    /// <summary>
    /// 読み（カタカナ想定）。
    /// </summary>
    public string Pronunciation { get; init; } = string.Empty;

    /// <summary>
    /// アクセント核位置。
    /// </summary>
    public int AccentType { get; init; }

    /// <summary>
    /// 単語種別。
    /// </summary>
    public WordType WordType { get; init; } = WordType.Unknown;

    /// <summary>
    /// 生成の信頼度。
    /// </summary>
    public ConfidenceLevel ConfidenceLevel { get; init; } = ConfidenceLevel.Medium;

    /// <summary>
    /// ユーザーが編集したかどうか。
    /// </summary>
    public bool IsEdited { get; init; }
}
