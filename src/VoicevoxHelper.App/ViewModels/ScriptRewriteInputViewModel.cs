using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.App.Views;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Core.Validators;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 台本リライト入力ViewModel。
/// </summary>
public sealed partial class ScriptRewriteInputViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly ILlmService _llmService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ScriptRewriteInputViewModel> _logger;
    private readonly ScriptValidator _validator = new();

    public ScriptRewriteInputViewModel(
        WorkflowState state,
        ILlmService llmService,
        INavigationService navigationService,
        ILogger<ScriptRewriteInputViewModel> logger)
    {
        _state = state;
        _llmService = llmService;
        _navigationService = navigationService;
        _logger = logger;
        ScriptText = _state.ScriptText;
    }

    [ObservableProperty]
    private string _scriptText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task RewriteAsync()
    {
        ErrorMessage = string.Empty;
        var script = new Script { Text = ScriptText };
        var validation = _validator.Validate(script);
        if (!validation.IsValid)
        {
            ErrorMessage = string.Join(Environment.NewLine, validation.Errors);
            return;
        }

        try
        {
            IsBusy = true;
            _state.ScriptText = ScriptText;
            _state.RewriteResult = await _llmService.RewriteScriptAsync(script, CancellationToken.None);
            _navigationService.Navigate<ScriptRewriteResultPage>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Rewrite failed: {Exception}", ex.ToString());
            ErrorMessage = "リライトに失敗しました。";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
