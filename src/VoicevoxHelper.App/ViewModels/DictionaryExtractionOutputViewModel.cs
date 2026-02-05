using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using VoicevoxHelper.App.Services;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// 辞書抽出出力ViewModel。
/// </summary>
public sealed partial class DictionaryExtractionOutputViewModel : ViewModelBase
{
    private readonly WorkflowState _state;
    private readonly ICsvParser _csvParser;
    private readonly IJsonParser _jsonParser;
    private readonly ILogger<DictionaryExtractionOutputViewModel> _logger;
    private readonly IFileDialogService _fileDialogService;

    public DictionaryExtractionOutputViewModel(
        WorkflowState state,
        ICsvParser csvParser,
        IJsonParser jsonParser,
        ILogger<DictionaryExtractionOutputViewModel> logger,
        IFileDialogService fileDialogService)
    {
        _state = state;
        _csvParser = csvParser;
        _jsonParser = jsonParser;
        _logger = logger;
        _fileDialogService = fileDialogService;
        Candidates = _state.DictionaryCandidates;
        OutputFormat = _state.OutputFormat;
        UpdateOutputText();
    }

    [ObservableProperty]
    private IReadOnlyList<DictionaryCandidate> _candidates = Array.Empty<DictionaryCandidate>();

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _outputFormat = "CSV";

    partial void OnOutputFormatChanged(string value)
    {
        _state.OutputFormat = value;
        UpdateOutputText();
    }

    [RelayCommand]
    private void SaveToFile()
    {
        try
        {
            var extension = OutputFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase) ? "json" : "csv";
            var defaultName = $"dictionary_{DateTime.Now:yyyy-MM-dd_HHmmss}.{extension}";
            var filter = extension.Equals("json", StringComparison.OrdinalIgnoreCase)
                ? "JSON ファイル (*.json)|*.json|すべてのファイル (*.*)|*.*"
                : "CSV ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*";
            var path = _fileDialogService.ShowSaveFileDialog(defaultName, filter);
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogInformation("User canceled saving output file.");
                return;
            }

            _logger.LogInformation("Saving {Format} output to {Path}", OutputFormat, path);
            File.WriteAllText(path, OutputText, new UTF8Encoding(true));
            _logger.LogInformation("Dictionary output saved to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed for output format {Format}", OutputFormat);
            ErrorMessage = "ファイル保存に失敗しました。";
        }
    }

    private void UpdateOutputText()
    {
        OutputText = OutputFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase)
            ? _jsonParser.Write(Candidates)
            : _csvParser.Write(Candidates);
    }
}
