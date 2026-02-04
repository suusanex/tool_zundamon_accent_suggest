using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 辞書登録実行ViewModel。
/// </summary>
public sealed partial class DictionaryRegistrationExecutionViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly IDictionaryRegistrationService _service;
    private readonly ILogger<DictionaryRegistrationExecutionViewModel> _logger;

    public DictionaryRegistrationExecutionViewModel(
        WorkflowState state,
        IDictionaryRegistrationService service,
        ILogger<DictionaryRegistrationExecutionViewModel> logger)
    {
        _state = state;
        _service = service;
        _logger = logger;
    }

    [ObservableProperty]
    private int _successCount;

    [ObservableProperty]
    private int _failureCount;

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            _logger.LogInformation("Starting registration for {CandidateCount} candidates", _state.DictionaryCandidates.Count);
            var report = await _service.RegisterAsync(_state.DictionaryCandidates, CancellationToken.None);
            SuccessCount = report.SuccessCount;
            FailureCount = report.FailureCount;
            ResultMessage = report.Status == ExecutionStatus.Success
                ? "登録が完了しました。"
                : "一部の登録に失敗しました。";
            _logger.LogInformation("Registration report status={Status} succeeded={Success} failed={Failure}", report.Status, report.SuccessCount, report.FailureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed after processing {CandidateCount} candidates", _state.DictionaryCandidates.Count);
            ErrorMessage = "辞書登録に失敗しました。";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
