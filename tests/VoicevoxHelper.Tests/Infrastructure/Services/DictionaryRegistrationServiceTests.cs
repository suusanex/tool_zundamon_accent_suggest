using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.Services;

namespace VoicevoxHelper.Tests.Infrastructure.Services;

[TestFixture]
public class DictionaryRegistrationServiceTests
{
    [Test]
    public async Task RegisterAsync_WhenUpdateExisting_IsCalled()
    {
        var client = new Mock<IVoicevoxApiClient>();
        client.Setup(c => c.GetUserDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, VoicevoxDictionaryEntry> { { "uuid", new VoicevoxDictionaryEntry { Surface = "A" } } });

        var settings = Options.Create(new VoicevoxSettings { UpdateExistingWords = true });
        var service = new DictionaryRegistrationService(client.Object, settings, NullLogger<DictionaryRegistrationService>.Instance);

        var candidates = new[] { new DictionaryCandidate { Surface = "A", Pronunciation = "エー", AccentType = 1 } };

        var report = await service.RegisterAsync(candidates, CancellationToken.None);

        client.Verify(c => c.UpdateWordAsync("uuid", It.IsAny<VoicevoxDictionaryEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(report.SuccessCount, Is.EqualTo(1));
    }

    [Test]
    public async Task RegisterAsync_WhenSkipExisting_DoesNotCallUpdate()
    {
        var client = new Mock<IVoicevoxApiClient>();
        client.Setup(c => c.GetUserDictionaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, VoicevoxDictionaryEntry> { { "uuid", new VoicevoxDictionaryEntry { Surface = "A" } } });

        var settings = Options.Create(new VoicevoxSettings { UpdateExistingWords = false });
        var service = new DictionaryRegistrationService(client.Object, settings, NullLogger<DictionaryRegistrationService>.Instance);

        var candidates = new[] { new DictionaryCandidate { Surface = "A", Pronunciation = "エー", AccentType = 1 } };

        var report = await service.RegisterAsync(candidates, CancellationToken.None);

        client.Verify(c => c.UpdateWordAsync(It.IsAny<string>(), It.IsAny<VoicevoxDictionaryEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(report.SuccessCount, Is.EqualTo(0));
    }
}
