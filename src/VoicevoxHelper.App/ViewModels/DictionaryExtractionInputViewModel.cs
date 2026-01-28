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
/// 辞書抽出入力ViewModel。
/// </summary>
public sealed partial class DictionaryExtractionInputViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly IMaskingService _maskingService;
    private readonly ILlmService _llmService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<DictionaryExtractionInputViewModel> _logger;
    private readonly ScriptValidator _validator = new();

    public DictionaryExtractionInputViewModel(
        WorkflowState state,
        IMaskingService maskingService,
        ILlmService llmService,
        INavigationService navigationService,
        ILogger<DictionaryExtractionInputViewModel> logger)
    {
        _state = state;
        _maskingService = maskingService;
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
    private async Task ExtractAsync()
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
            var masked = _maskingService.Mask(ScriptText);
            var maskedScript = new Script { Text = masked.MaskedText };
            var candidates = await _llmService.ExtractDictionaryCandidatesAsync(maskedScript, CancellationToken.None);
            _state.DictionaryCandidates = candidates;

            if (!_navigationService.Navigate<DictionaryExtractionPreviewPage>())
            {
                ErrorMessage = "画面遷移に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Extraction failed: {Exception}", ex.ToString());
            ErrorMessage = "辞書候補の抽出に失敗しました。";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
