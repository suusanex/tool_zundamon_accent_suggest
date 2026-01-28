using System.Windows.Controls;
using VoicevoxHelper.App.ViewModels;

namespace VoicevoxHelper.App.Views;

/// <summary>
/// 台本リライト結果ページ。
/// </summary>
public partial class ScriptRewriteResultPage : Page
{
    public ScriptRewriteResultPage(ScriptRewriteResultViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
