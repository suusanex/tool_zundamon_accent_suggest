namespace VoicevoxHelper.Core.Models;

/// <summary>
/// アプリケーション設定。
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// LLM設定。
    /// </summary>
    public LlmSettings Llm { get; init; } = new();

    /// <summary>
    /// VOICEVOX設定。
    /// </summary>
    public VoicevoxSettings Voicevox { get; init; } = new();

    /// <summary>
    /// 辞書設定。
    /// </summary>
    public DictionarySettings Dictionary { get; init; } = new();
}

/// <summary>
/// LLM設定。
/// </summary>
public sealed class LlmSettings
{
    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Deployment { get; init; } = string.Empty;
    public string ApiVersion { get; init; } = "2024-02-15-preview";
    public int TimeoutSeconds { get; init; } = 60;
    public decimal PromptCostPer1kTokens { get; init; } = 0m;
    public decimal CompletionCostPer1kTokens { get; init; } = 0m;
}

/// <summary>
/// VOICEVOX設定。
/// </summary>
public sealed class VoicevoxSettings
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 60;
    public bool UpdateExistingWords { get; init; } = true;
}

/// <summary>
/// 辞書設定。
/// </summary>
public sealed class DictionarySettings
{
    public bool UseCsvAsDefault { get; init; } = true;
}
