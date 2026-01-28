using NUnit.Framework;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Core.Validators;

namespace VoicevoxHelper.Tests.Core.Validators;

[TestFixture]
public class DictionaryCandidateValidatorTests
{
    [Test]
    public void Validate_WhenCandidateIsNull_ReturnsFailure()
    {
        var validator = new DictionaryCandidateValidator();

        var result = validator.Validate(null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_WhenCandidateIsValid_ReturnsSuccess()
    {
        var validator = new DictionaryCandidateValidator();
        var candidate = new DictionaryCandidate
        {
            Surface = "VOICEVOX",
            Pronunciation = "ボイスボックス",
            AccentType = 1
        };

        var result = validator.Validate(candidate);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_WhenCandidateHasErrors_ReturnsFailure()
    {
        var validator = new DictionaryCandidateValidator();
        var candidate = new DictionaryCandidate
        {
            Surface = "",
            Pronunciation = "",
            AccentType = -1
        };

        var result = validator.Validate(candidate);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(3));
    }
}
