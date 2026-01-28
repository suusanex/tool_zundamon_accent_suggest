using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// モード選択ページ。
/// </summary>
public partial class ModeSelectionPage : Page
{
    public ModeSelectionPage(ModeSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
