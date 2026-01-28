using NUnit.Framework;
using VoicevoxHelper.Infrastructure.Masking;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Tests.Infrastructure.Masking;

[TestFixture]
public class PersonalInfoMaskingServiceTests
{
    [Test]
    public void Mask_WhenEmailExists_ReplacesEmail()
    {
        var service = new PersonalInfoMaskingService();
        var text = "連絡先は test@example.com です。";

        var result = service.Mask(text);

        Assert.That(result.MaskedText, Does.Contain("[MASKED_EMAIL]"));
        Assert.That(result.MaskingType, Is.Not.EqualTo(MaskingType.None));
    }

    [Test]
    public void Mask_WhenPhoneExists_ReplacesPhone()
    {
        var service = new PersonalInfoMaskingService();
        var text = "電話は 03-1234-5678 です。";

        var result = service.Mask(text);

        Assert.That(result.MaskedText, Does.Contain("[MASKED_PHONE]"));
    }
}
