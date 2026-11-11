using System.Collections.Frozen;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Views;

public sealed class ProfileColorToBrushConverter : IValueConverter
{
    private static readonly FrozenDictionary<ProfileColor, IImmutableSolidColorBrush> BrushesByColor = Enum
        .GetValues<ProfileColor>()
        .ToFrozenDictionary(
            color => color,
            IImmutableSolidColorBrush (color) => new ImmutableSolidColorBrush(ProfilePalette.ColorOf(color)));

    public static ProfileColorToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ProfileColor color ? BrushesByColor[color] : AvaloniaProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
