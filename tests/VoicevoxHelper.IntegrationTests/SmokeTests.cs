using NUnit.Framework;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.IntegrationTests;

[TestFixture]
public class SmokeTests
{
    [Test]
    public void ApplicationSettings_Defaults_AreInitialized()
    {
        var settings = new ApplicationSettings();

        Assert.That(settings.Llm, Is.Not.Null);
        Assert.That(settings.Voicevox, Is.Not.Null);
        Assert.That(settings.Dictionary, Is.Not.Null);
    }
}
