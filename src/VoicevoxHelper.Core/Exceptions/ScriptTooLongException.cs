namespace VoicevoxHelper.Core.Exceptions;

/// <summary>
/// 台本の文字数上限超過を示す例外。
/// </summary>
public sealed class ScriptTooLongException : Exception
{
    public ScriptTooLongException(string message)
        : base(message)
    {
    }

    public ScriptTooLongException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
