using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 辞書登録ファイル選択ページ。
/// </summary>
public partial class DictionaryRegistrationFileSelectionPage : Page
{
    public DictionaryRegistrationFileSelectionPage(DictionaryRegistrationFileSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
