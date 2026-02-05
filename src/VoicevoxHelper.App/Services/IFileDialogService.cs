namespace VoicevoxHelper.App.Services;

/// <summary>
/// ファイルダイアログの抽象化インターフェイス。
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// 保存先ファイルのパスを問い合わせる。
    /// </summary>
    /// <param name="defaultFileName">初期ファイル名。</param>
    /// <param name="filter">ファイルフィルター。</param>
    /// <returns>選択されたファイルパス。キャンセル時は null。</returns>
    string? ShowSaveFileDialog(string defaultFileName, string filter);

    /// <summary>
    /// 読み込みファイルのパスを問い合わせる。
    /// </summary>
    /// <param name="filter">ファイルフィルター。</param>
    /// <returns>選択されたファイルパス。キャンセル時は null。</returns>
    string? ShowOpenFileDialog(string filter);
}
