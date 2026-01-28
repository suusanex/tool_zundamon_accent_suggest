using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 台本リライト入力ページ。
/// </summary>
public partial class ScriptRewriteInputPage : Page
{
    public ScriptRewriteInputPage(ScriptRewriteInputViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
