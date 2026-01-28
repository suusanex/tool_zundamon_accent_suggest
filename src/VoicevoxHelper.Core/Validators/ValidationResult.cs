namespace VoicevoxHelper.Core.Validators;

/// <summary>
/// バリデーション結果。
/// </summary>
public sealed class ValidationResult
{
    private ValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// 成功かどうか。
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// エラー一覧。
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    public static ValidationResult Success() => new(true, Array.Empty<string>());

    public static ValidationResult Failure(IEnumerable<string> errors)
        => new(false, errors.ToArray());
}
