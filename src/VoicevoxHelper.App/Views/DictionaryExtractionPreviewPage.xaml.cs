using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 辞書抽出プレビューページ。
/// </summary>
public partial class DictionaryExtractionPreviewPage : Page
{
    public DictionaryExtractionPreviewPage(DictionaryExtractionPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
