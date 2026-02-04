using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.App.Views;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Core.Validators;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 辞書登録ファイル選択ViewModel。
/// </summary>
public sealed partial class DictionaryRegistrationFileSelectionViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly ICsvParser _csvParser;
    private readonly IJsonParser _jsonParser;
    private readonly INavigationService _navigationService;
    private readonly ILogger<DictionaryRegistrationFileSelectionViewModel> _logger;
    private readonly DictionaryCandidateValidator _validator = new();

    public DictionaryRegistrationFileSelectionViewModel(
        WorkflowState state,
        ICsvParser csvParser,
        IJsonParser jsonParser,
        INavigationService navigationService,
        ILogger<DictionaryRegistrationFileSelectionViewModel> logger)
    {
        _state = state;
        _csvParser = csvParser;
        _jsonParser = jsonParser;
        _navigationService = navigationService;
        _logger = logger;
        FilePath = _state.SelectedFilePath;
    }

    [ObservableProperty]
    private string _filePath = string.Empty;

    [RelayCommand]
    private void Load()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            _logger.LogWarning("Dictionary file not found: {FilePath}", FilePath);
            ErrorMessage = "ファイルが見つかりません。";
            return;
        }

        try
        {
            _logger.LogInformation("Loading dictionary file {FilePath}", FilePath);
            var text = File.ReadAllText(FilePath);
            IReadOnlyList<DictionaryCandidate> candidates;
            if (Path.GetExtension(FilePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Parsing JSON dictionary file {FilePath}", FilePath);
                candidates = _jsonParser.Read(text);
            }
            else
            {
                _logger.LogInformation("Parsing CSV dictionary file {FilePath}", FilePath);
                candidates = _csvParser.Read(text);
            }

            var errors = new List<string>();
            foreach (var candidate in candidates)
            {
                var result = _validator.Validate(candidate);
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Count > 0)
            {
                ErrorMessage = string.Join(Environment.NewLine, errors);
                return;
            }

            _state.SelectedFilePath = FilePath;
            _state.DictionaryCandidates = candidates;
            _logger.LogInformation("Loaded {CandidateCount} candidates from {FilePath}", candidates.Count, FilePath);
            _navigationService.Navigate<DictionaryRegistrationValidationPage>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File load failed: {FilePath}", FilePath);
            ErrorMessage = "ファイルの読み込みに失敗しました。";
        }
    }
}
