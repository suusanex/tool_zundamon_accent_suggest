namespace VoicevoxHelper.Core.Exceptions;

/// <summary>
/// VOICEVOX APIエラーを示す例外。
/// </summary>
public sealed class VoicevoxApiException : Exception
{
    public VoicevoxApiException(string message)
        : base(message)
    {
    }

    public VoicevoxApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
