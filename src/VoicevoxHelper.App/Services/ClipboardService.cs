using System.Windows;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App.Services;

/// <summary>
/// クリップボードサービス。
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
