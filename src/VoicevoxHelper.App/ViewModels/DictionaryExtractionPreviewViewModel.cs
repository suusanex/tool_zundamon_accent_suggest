using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.App.Views;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 辞書抽出プレビューViewModel。
/// </summary>
public sealed partial class DictionaryExtractionPreviewViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly INavigationService _navigationService;
    private readonly ILogger<DictionaryExtractionPreviewViewModel> _logger;

    public DictionaryExtractionPreviewViewModel(
        WorkflowState state,
        INavigationService navigationService,
        ILogger<DictionaryExtractionPreviewViewModel> logger)
    {
        _state = state;
        _navigationService = navigationService;
        _logger = logger;
        Candidates = _state.DictionaryCandidates;
    }

    [ObservableProperty]
    private IReadOnlyList<DictionaryCandidate> _candidates = Array.Empty<DictionaryCandidate>();

    [RelayCommand]
    private void Back()
    {
        try
        {
            _navigationService.Navigate<DictionaryExtractionInputPage>();
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
            _navigationService.Navigate<DictionaryExtractionOutputPage>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            ErrorMessage = "画面遷移に失敗しました。";
        }
    }
}
