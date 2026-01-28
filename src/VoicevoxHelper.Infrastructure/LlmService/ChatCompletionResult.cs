namespace VoicevoxHelper.Infrastructure.LlmService;

/// <summary>
/// チャット補完結果。
/// </summary>
public sealed class ChatCompletionResult
{
    public string Content { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
}
