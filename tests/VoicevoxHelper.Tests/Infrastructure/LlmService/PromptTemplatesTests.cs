using NUnit.Framework;
using VoicevoxHelper.Infrastructure.LlmService;

namespace VoicevoxHelper.Tests.Infrastructure.LlmService;

[TestFixture]
public class PromptTemplatesTests
{
    [Test]
    public void DictionaryExtractionSystem_IncludesJapaneseExamplesAndAccentType()
    {
        var prompt = PromptTemplates.DictionaryExtractionSystem;

        Assert.That(prompt, Does.Contain("accent_type"));
        Assert.That(prompt, Does.Contain("\"candidates\""));
        Assert.That(prompt, Does.Contain("東京"));
        Assert.That(prompt, Does.Contain("VOICEVOX"));
    }
}
