using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.App.Views;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 辞書登録バリデーションViewModel。
/// </summary>
public sealed partial class DictionaryRegistrationValidationViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly INavigationService _navigationService;
    private readonly ILogger<DictionaryRegistrationValidationViewModel> _logger;

    public DictionaryRegistrationValidationViewModel(
        WorkflowState state,
        INavigationService navigationService,
        ILogger<DictionaryRegistrationValidationViewModel> logger)
    {
        _state = state;
        _navigationService = navigationService;
        _logger = logger;
        CandidateCount = _state.DictionaryCandidates.Count;
    }

    [ObservableProperty]
    private int _candidateCount;

    [RelayCommand]
    private void Back()
    {
        try
        {
            _navigationService.Navigate<DictionaryRegistrationFileSelectionPage>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            ErrorMessage = "画面遷移に失敗しました。";
        }
    }

    [RelayCommand]
    private void Next()
    {
        try
        {
            _navigationService.Navigate<DictionaryRegistrationExecutionPage>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            ErrorMessage = "画面遷移に失敗しました。";
        }
    }
}
