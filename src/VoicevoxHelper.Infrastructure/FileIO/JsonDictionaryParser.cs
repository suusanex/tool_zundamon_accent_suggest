using System.Text.Json;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.FileIO;

/// <summary>
/// JSON辞書パーサー。
/// </summary>
public sealed class JsonDictionaryParser : IJsonParser
{
    public IReadOnlyList<DictionaryCandidate> Read(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            return Array.Empty<DictionaryCandidate>();
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var items = JsonSerializer.Deserialize<List<DictionaryCandidate>>(jsonText, options);
            return items ?? new List<DictionaryCandidate>();
        }
        catch (JsonException ex)
        {
            var line = ex.LineNumber ?? 0;
            var position = ex.BytePositionInLine ?? 0;
            throw new FormatException($"JSONパースエラー: 行 {line} / 位置 {position}", ex);
        }
    }

    public string Write(IReadOnlyList<DictionaryCandidate> candidates)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(candidates, options);
    }
}
