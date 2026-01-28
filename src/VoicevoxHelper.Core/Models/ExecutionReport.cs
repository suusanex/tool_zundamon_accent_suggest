namespace VoicevoxHelper.Core.Models;

/// <summary>
/// 実行結果レポート。
/// </summary>
public sealed class ExecutionReport
{
    /// <summary>
    /// 実行モード。
    /// </summary>
    public ExecutionMode Mode { get; init; }

    /// <summary>
    /// 実行ステータス。
    /// </summary>
    public ExecutionStatus Status { get; init; } = ExecutionStatus.Unknown;

    /// <summary>
    /// 成功件数。
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失敗件数。
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// 実行時間。
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// プロンプトトークン数。
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// 完了トークン数。
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// 合計トークン数。
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// 失敗詳細。
    /// </summary>
    public IReadOnlyList<string> FailureDetails { get; init; } = Array.Empty<string>();

    /// <summary>
    /// エラーメッセージ。
    /// </summary>
    public string? ErrorMessage { get; init; }
}
