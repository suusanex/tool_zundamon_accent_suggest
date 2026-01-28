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
            _clipboardService.SetText(RewriteResult);
        }
        catch (Exception ex)
        {
            _logger.LogError("Copy failed: {Exception}", ex.ToString());
            ErrorMessage = "コピーに失敗しました。";
        }
    }
}
