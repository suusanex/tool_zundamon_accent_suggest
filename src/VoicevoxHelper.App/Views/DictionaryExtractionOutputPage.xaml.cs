using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 辞書抽出出力ページ。
/// </summary>
public partial class DictionaryExtractionOutputPage : Page
{
    public DictionaryExtractionOutputPage(DictionaryExtractionOutputViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
