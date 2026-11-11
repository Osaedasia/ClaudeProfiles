using System.Collections.Frozen;
using Avalonia.Media;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Views;

public static class ProfilePalette
{
    private static readonly FrozenDictionary<ProfileColor, Color> ColorsByProfileColor =
        new Dictionary<ProfileColor, Color>
        {
            [ProfileColor.Clay] = Color.Parse("#D97757"),
            [ProfileColor.Sky] = Color.Parse("#6A9BCC"),
            [ProfileColor.Olive] = Color.Parse("#788C5D"),
            [ProfileColor.Plum] = Color.Parse("#9B7AB8"),
            [ProfileColor.Sand] = Color.Parse("#C4A26A"),
            [ProfileColor.Slate] = Color.Parse("#6B7280")
        }.ToFrozenDictionary();

    public static Color ColorOf(ProfileColor color) => ColorsByProfileColor[color];
}
