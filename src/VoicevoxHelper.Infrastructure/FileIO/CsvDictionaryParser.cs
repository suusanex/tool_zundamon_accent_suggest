using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.FileIO;

/// <summary>
/// CSV辞書パーサー。
/// </summary>
public sealed class CsvDictionaryParser : ICsvParser
{
    private sealed class CsvEntry
    {
        public string Surface { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public int AccentType { get; set; }
    }

    private sealed class CsvEntryMap : ClassMap<CsvEntry>
    {
        public CsvEntryMap()
        {
            Map(m => m.Surface).Name("surface");
            Map(m => m.Pronunciation).Name("pronunciation");
            Map(m => m.AccentType).Name("accent_type");
        }
    }

    public IReadOnlyList<DictionaryCandidate> Read(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return Array.Empty<DictionaryCandidate>();
        }

        try
        {
            using var reader = new StringReader(csvText);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CsvEntryMap>();
            var results = new List<DictionaryCandidate>();
            foreach (var entry in csv.GetRecords<CsvEntry>())
            {
                results.Add(new DictionaryCandidate
                {
                    Surface = entry.Surface,
                    Pronunciation = entry.Pronunciation,
                    AccentType = entry.AccentType
                });
            }
            return results;
        }
        catch (CsvHelperException ex)
        {
            var row = ex.Context?.Parser?.RawRow ?? 0;
            var message = $"CSVパースエラー: 行 {row}";
            throw new FormatException(message, ex);
        }
    }

    public string Write(IReadOnlyList<DictionaryCandidate> candidates)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvEntryMap>();
        csv.WriteHeader<CsvEntry>();
        csv.NextRecord();

        foreach (var candidate in candidates)
        {
            csv.WriteRecord(new CsvEntry
            {
                Surface = candidate.Surface,
                Pronunciation = candidate.Pronunciation,
                AccentType = candidate.AccentType
            });
            csv.NextRecord();
        }

        return writer.ToString();
    }
}
