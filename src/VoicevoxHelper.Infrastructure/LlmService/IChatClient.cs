namespace VoicevoxHelper.Infrastructure.LlmService;

/// <summary>
/// チャット補完クライアント。
/// </summary>
public interface IChatClient
{
    Task<ChatCompletionResult> GetChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        bool requireJson = false);
}
