using System.Text.RegularExpressions;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.Masking;

/// <summary>
/// 正規表現ベースの個人情報マスキング。
/// </summary>
public sealed class PersonalInfoMaskingService : IMaskingService
{
    private static readonly Regex EmailRegex =
        new(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PhoneRegex =
        new(@"\b\d{2,4}-\d{2,4}-\d{3,4}\b", RegexOptions.Compiled);

    private static readonly Regex AddressRegex =
        new(@"(都|道|府|県).+?(市|区|町|村)", RegexOptions.Compiled);

    public MaskingResult Mask(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var maskedText = text;
        var maskingType = MaskingType.None;

        if (EmailRegex.IsMatch(maskedText))
        {
            maskedText = EmailRegex.Replace(maskedText, "[MASKED_EMAIL]");
            maskingType = MaskingType.Email;
        }

        if (PhoneRegex.IsMatch(maskedText))
        {
            maskedText = PhoneRegex.Replace(maskedText, "[MASKED_PHONE]");
            maskingType = maskingType == MaskingType.None ? MaskingType.PhoneNumber : MaskingType.Other;
        }

        if (AddressRegex.IsMatch(maskedText))
        {
            maskedText = AddressRegex.Replace(maskedText, "[MASKED_ADDRESS]");
            maskingType = maskingType == MaskingType.None ? MaskingType.Address : MaskingType.Other;
        }

        return new MaskingResult
        {
            OriginalLength = text.Length,
            MaskedLength = maskedText.Length,
            MaskingType = maskingType,
            MaskedText = maskedText
        };
    }
}
