using NUnit.Framework;
using VoicevoxHelper.Infrastructure.LlmService;

namespace VoicevoxHelper.Tests.Infrastructure.LlmService;

[TestFixture]
public class PromptTemplatesTests
{
    [Test]
    public void DictionaryExtractionSystem_ContainsConstraint()
    {
        Assert.That(PromptTemplates.DictionaryExtractionSystem, Does.Contain("辞書候補抽出のみ"));
    }
}
