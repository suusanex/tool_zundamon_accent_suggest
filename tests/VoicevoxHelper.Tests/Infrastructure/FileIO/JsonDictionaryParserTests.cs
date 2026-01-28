using NUnit.Framework;
using VoicevoxHelper.Infrastructure.FileIO;

namespace VoicevoxHelper.Tests.Infrastructure.FileIO;

[TestFixture]
public class JsonDictionaryParserTests
{
    [Test]
    public void Read_WhenValidJson_ReturnsCandidates()
    {
        var parser = new JsonDictionaryParser();
        var json = "[{\"surface\":\"VOICEVOX\",\"pronunciation\":\"ボイスボックス\",\"accentType\":1}]";

        var result = parser.Read(json);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Pronunciation, Is.EqualTo("ボイスボックス"));
    }

    [Test]
    public void Write_WhenCandidatesProvided_OutputIsJsonArray()
    {
        var parser = new JsonDictionaryParser();
        var output = parser.Write(new[] { new VoicevoxHelper.Core.Models.DictionaryCandidate { Surface = "A", Pronunciation = "エー", AccentType = 1 } });

        Assert.That(output, Does.StartWith("["));
    }
}
