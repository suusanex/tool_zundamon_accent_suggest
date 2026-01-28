using VoicevoxHelper.Core.Exceptions;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Core.Validators;

/// <summary>
/// 台本のバリデーション。
/// </summary>
public sealed class ScriptValidator
{
    public const int MaxLength = 10_000;

    public const string SplitGuidanceMessage =
        "台本が10,000文字を超えています。章ごとに分割し、1回の処理は10,000文字以内にしてください。";

    public ValidationResult Validate(Script? script)
    {
        if (script is null)
        {
            return ValidationResult.Failure(new[] { "台本が指定されていません。" });
        }

        if (string.IsNullOrWhiteSpace(script.Text))
        {
            return ValidationResult.Failure(new[] { "台本の内容が空です。" });
        }

        if (script.Length > MaxLength)
        {
            return ValidationResult.Failure(new[] { SplitGuidanceMessage });
        }

        return ValidationResult.Success();
    }

    public void EnsureWithinLimit(Script? script)
    {
        if (script is null)
        {
            throw new ArgumentNullException(nameof(script));
        }

        if (script.Length > MaxLength)
        {
            throw new ScriptTooLongException(SplitGuidanceMessage);
        }
    }
}
