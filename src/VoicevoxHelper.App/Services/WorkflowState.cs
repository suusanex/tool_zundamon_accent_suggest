using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.App.Services;

/// <summary>
/// ワークフロー状態。
/// </summary>
public sealed class WorkflowState
{
    public string ScriptText { get; set; } = string.Empty;

    public string RewriteResult { get; set; } = string.Empty;

    public IReadOnlyList<DictionaryCandidate> DictionaryCandidates { get; set; } = Array.Empty<DictionaryCandidate>();

    public string SelectedFilePath { get; set; } = string.Empty;

    public string OutputFormat { get; set; } = "CSV";
}
