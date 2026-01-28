using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 辞書登録バリデーションページ。
/// </summary>
public partial class DictionaryRegistrationValidationPage : Page
{
    public DictionaryRegistrationValidationPage(DictionaryRegistrationValidationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
