using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.Services;

namespace VoicevoxHelper.IntegrationTests;

[TestFixture]
public class DictionaryRegistrationFlowTests
{
    [Test]
    public async Task RegistrationFlow_ReturnsReport()
    {
        var client = new Mock<IVoicevoxApiClient>();
        client.Setup(c => c.GetUserDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, VoicevoxDictionaryEntry>());
        client.Setup(c => c.CreateWordAsync(It.IsAny<VoicevoxDictionaryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("uuid");

        var service = new DictionaryRegistrationService(client.Object, Options.Create(new VoicevoxSettings()), NullLogger<DictionaryRegistrationService>.Instance);
        var report = await service.RegisterAsync(new[]
        {
            new DictionaryCandidate { Surface = "A", Pronunciation = "エー", AccentType = 1 }
        }, CancellationToken.None);

        Assert.That(report.SuccessCount, Is.EqualTo(1));
    }
}
