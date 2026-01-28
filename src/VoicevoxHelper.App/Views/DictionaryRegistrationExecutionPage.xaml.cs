using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 辞書登録実行ページ。
/// </summary>
public partial class DictionaryRegistrationExecutionPage : Page
{
    public DictionaryRegistrationExecutionPage(DictionaryRegistrationExecutionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
