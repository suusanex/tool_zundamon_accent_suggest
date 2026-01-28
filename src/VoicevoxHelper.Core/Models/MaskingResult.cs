namespace VoicevoxHelper.Core.Models;

/// <summary>
/// マスキング処理の結果。
/// </summary>
public sealed class MaskingResult
{
    /// <summary>
    /// 元の文字数。
    /// </summary>
    public int OriginalLength { get; init; }

    /// <summary>
    /// マスキング後の文字数。
    /// </summary>
    public int MaskedLength { get; init; }

    /// <summary>
    /// マスキング種別。
    /// </summary>
    public MaskingType MaskingType { get; init; }

    /// <summary>
    /// マスキング済みテキスト。
    /// </summary>
    public string MaskedText { get; init; } = string.Empty;
}
