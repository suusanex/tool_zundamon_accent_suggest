using System.IO;
using Microsoft.Win32;

namespace VoicevoxHelper.App.Services;

/// <summary>
/// ファイルダイアログを提供する既定実装。
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? ShowSaveFileDialog(string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = filter,
            AddExtension = true,
            DefaultExt = Path.GetExtension(defaultFileName)
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? ShowOpenFileDialog(string filter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
