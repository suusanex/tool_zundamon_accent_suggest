using NUnit.Framework;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Core.Validators;

namespace VoicevoxHelper.Tests.Core.Validators;

[TestFixture]
public class ScriptValidatorTests
{
    [Test]
    public void Validate_WhenScriptIsNull_ReturnsFailure()
    {
        var validator = new ScriptValidator();

        var result = validator.Validate(null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_WhenScriptIsEmpty_ReturnsFailure()
    {
        var validator = new ScriptValidator();
        var script = new Script { Text = "" };

        var result = validator.Validate(script);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_WhenScriptIsTooLong_ReturnsGuidanceMessage()
    {
        var validator = new ScriptValidator();
        var script = new Script { Text = new string('あ', ScriptValidator.MaxLength + 1) };

        var result = validator.Validate(script);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0], Is.EqualTo(ScriptValidator.SplitGuidanceMessage));
    }

    [Test]
    public void Validate_WhenScriptIsValid_ReturnsSuccess()
    {
        var validator = new ScriptValidator();
        var script = new Script { Text = "これはテスト用の台本です。" };

        var result = validator.Validate(script);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }
}
