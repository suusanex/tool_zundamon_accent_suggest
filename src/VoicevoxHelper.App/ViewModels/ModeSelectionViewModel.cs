using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.App.Views;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// モード選択ViewModel。
/// </summary>
public sealed partial class ModeSelectionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ILogger<ModeSelectionViewModel> _logger;

    public ModeSelectionViewModel(INavigationService navigationService, ILogger<ModeSelectionViewModel> logger)
    {
        _navigationService = navigationService;
        _logger = logger;
    }

    [RelayCommand]
    private void NavigateDictionaryExtraction()
    {
        try
        {
            if (!_navigationService.Navigate<DictionaryExtractionInputPage>())
            {
                ErrorMessage = "画面遷移に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            ErrorMessage = "画面遷移に失敗しました。";
        }
    }

    [RelayCommand]
    private void NavigateDictionaryRegistration()
    {
        try
        {
            if (!_navigationService.Navigate<DictionaryRegistrationFileSelectionPage>())
            {
                ErrorMessage = "画面遷移に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            ErrorMessage = "画面遷移に失敗しました。";
        }
    }

    [RelayCommand]
    private void NavigateScriptRewrite()
    {
        try
        {
            if (!_navigationService.Navigate<ScriptRewriteInputPage>())
            {
                ErrorMessage = "画面遷移に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            ErrorMessage = "画面遷移に失敗しました。";
        }
    }
}
