namespace VoicevoxHelper.Core.Exceptions;

/// <summary>
/// LLMレスポンスの解析失敗を示す例外。
/// </summary>
public sealed class LlmResponseParseException : Exception
{
    public string? RawResponse { get; }

    public LlmResponseParseException(string message)
        : base(message)
    {
    }

    public LlmResponseParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LlmResponseParseException(string message, string rawResponse, Exception innerException)
        : base(message, innerException)
    {
        RawResponse = rawResponse;
    }
}
