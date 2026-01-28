using NUnit.Framework;
using VoicevoxHelper.Infrastructure.FileIO;

namespace VoicevoxHelper.Tests.Infrastructure.FileIO;

[TestFixture]
public class CsvDictionaryParserTests
{
    [Test]
    public void Read_WhenValidCsv_ReturnsCandidates()
    {
        var parser = new CsvDictionaryParser();
        var csv = "surface,pronunciation,accent_type\nVOICEVOX,ボイスボックス,1";

        var result = parser.Read(csv);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Surface, Is.EqualTo("VOICEVOX"));
    }

    [Test]
    public void Write_WhenCandidatesProvided_OutputContainsHeader()
    {
        var parser = new CsvDictionaryParser();
        var output = parser.Write(new[] { new VoicevoxHelper.Core.Models.DictionaryCandidate { Surface = "A", Pronunciation = "エー", AccentType = 1 } });

        Assert.That(output, Does.Contain("surface"));
        Assert.That(output, Does.Contain("accent_type"));
    }
}
