using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 辞書抽出入力ページ。
/// </summary>
public partial class DictionaryExtractionInputPage : Page
{
    public DictionaryExtractionInputPage(DictionaryExtractionInputViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
