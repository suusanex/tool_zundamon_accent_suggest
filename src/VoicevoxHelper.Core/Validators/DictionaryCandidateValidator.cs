using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Validators;

/// <summary>
/// 辞書候補のバリデーション。
/// </summary>
public sealed class DictionaryCandidateValidator
{
    public ValidationResult Validate(DictionaryCandidate? candidate)
    {
        if (candidate is null)
        {
            return ValidationResult.Failure(new[] { "辞書候補が指定されていません。" });
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(candidate.Surface))
        {
            errors.Add("単語表記が空です。");
        }

        if (string.IsNullOrWhiteSpace(candidate.Pronunciation))
        {
            errors.Add("読みが空です。");
        }

        if (candidate.AccentType < 0)
        {
            errors.Add("アクセント核位置は0以上で指定してください。");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
