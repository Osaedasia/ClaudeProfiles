using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeProfiles.Core.Profiles;

public static partial class ProfileFolderName
{
    private const string FallbackSlug = "Profile";

    public static string ToSlug(string displayName)
    {
        string withoutAccents = RemoveDiacritics(displayName);
        string slug = NonAlphanumericRuns().Replace(withoutAccents, "-").Trim('-');

        return slug.Length == 0 ? FallbackSlug : slug;
    }

    private static string RemoveDiacritics(string text)
    {
        string decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex NonAlphanumericRuns();
}
