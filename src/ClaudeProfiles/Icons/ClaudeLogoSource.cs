using System.Text.RegularExpressions;

namespace ClaudeProfiles.Icons;

internal sealed partial class ClaudeLogoSource
{
    private const string LogoFilePattern = "Square44x44Logo.targetsize-*.png";

    private readonly SortedDictionary<int, string> _logoFileBySize = [];

    public ClaudeLogoSource(string? logoDirectory)
    {
        if (logoDirectory is null || !Directory.Exists(logoDirectory))
        {
            return;
        }

        foreach (string logoFile in Directory.EnumerateFiles(logoDirectory, LogoFilePattern))
        {
            Match sizeMatch = PlainLogoFileName().Match(Path.GetFileName(logoFile));

            if (sizeMatch.Success)
            {
                _logoFileBySize[int.Parse(sizeMatch.Groups["size"].Value)] = logoFile;
            }
        }
    }

    public bool IsAvailable => _logoFileBySize.Count > 0;

    public string? FindLogoFor(int size)
    {
        if (!IsAvailable)
        {
            return null;
        }

        KeyValuePair<int, string> smallestLargeEnough = _logoFileBySize.FirstOrDefault(entry => entry.Key >= size);
        return smallestLargeEnough.Value ?? _logoFileBySize.Last().Value;
    }

    [GeneratedRegex(@"^Square44x44Logo\.targetsize-(?<size>\d+)\.png$")]
    private static partial Regex PlainLogoFileName();
}
