using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// 個人情報マスキングサービス。
/// </summary>
public interface IMaskingService
{
    /// <summary>
    /// テキストをマスキングする。
    /// </summary>
    /// <param name="text">対象テキスト。</param>
    /// <returns>マスキング結果。</returns>
    MaskingResult Mask(string text);
}
