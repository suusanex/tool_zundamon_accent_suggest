using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 台本リライト結果ViewModel。
/// </summary>
public sealed partial class ScriptRewriteResultViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<ScriptRewriteResultViewModel> _logger;

    public ScriptRewriteResultViewModel(
        WorkflowState state,
        IClipboardService clipboardService,
        ILogger<ScriptRewriteResultViewModel> logger)
    {
        _state = state;
        _clipboardService = clipboardService;
        _logger = logger;
        OriginalScript = _state.ScriptText;
        RewriteResult = _state.RewriteResult;
    }

    [ObservableProperty]
    private string _originalScript = string.Empty;

    [ObservableProperty]
    private string _rewriteResult = string.Empty;

    [RelayCommand]
    private void Copy()
    {
        try
        {
            _logger.LogInformation("Copying rewrite result of length {Length}", RewriteResult.Length);
            _clipboardService.SetText(RewriteResult);
            _logger.LogInformation("Rewrite result copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed for rewrite result");
            ErrorMessage = "コピーに失敗しました。";
        }
    }
}
