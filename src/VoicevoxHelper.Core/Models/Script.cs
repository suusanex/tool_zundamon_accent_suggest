namespace VoicevoxHelper.Core.Models;

/// <summary>
/// 台本を表すモデル。
/// </summary>
public sealed class Script
{
    /// <summary>
    /// 台本文。
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 文字数。
    /// </summary>
    public int Length => Text?.Length ?? 0;
}
