using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClaudeProfiles.Core.Platform;
using ClaudeProfiles.Core.Profiles;
using ClaudeProfiles.Views;

namespace ClaudeProfiles.Icons;

public sealed class AvaloniaProfileIconFactory : IProfileIconFactory
{
    private const int SmallestSizeWithInitialBadge = 24;
    private const double BadgeDiameterRatio = 0.54;
    private const double TileCornerRatio = 0.22;
    private const double TileInitialSizeRatio = 0.62;
    private const double BadgeInitialSizeRatio = 1.25;
    private const double SizePerRingPixel = 24;
    private const int BytesPerPixel = 4;
    private const int AlphaOffset = 3;
    private const int GreenOffset = 1;
    private const double LogoBackgroundBlueLevel = 90;
    private const double LogoForegroundBlueLevel = 235;
    private const string IconNameSeparator = "__";

    // L'Explorateur Windows garde en cache, pour la session, l'icône lue à un chemin donné, y compris un échec de
    // lecture : modifier le dessin sans changer le nom du fichier laisserait l'ancienne image (ou une icône vide)
    // affichée dans la barre des tâches. À incrémenter à chaque changement du rendu.
    private const int ArtworkVersion = 2;

    private static readonly int[] TaskbarIconSizes = [16, 20, 24, 32, 40, 48, 64, 256];
    private static readonly int[] TrayIconSizes = [16, 20, 24, 32, 40, 48];
    private static readonly Color Ivory = Color.Parse("#FAF9F5");

    private readonly string _iconsDirectory;

    public AvaloniaProfileIconFactory(string iconsDirectory)
    {
        _iconsDirectory = iconsDirectory;
    }

    public ProfileIconSet GetIcons(ClaudeProfile profile, string? claudeLogoDirectory)
    {
        var logos = new ClaudeLogoSource(claudeLogoDirectory);
        string iconBaseName = BuildIconBaseName(profile, logos.IsAvailable);
        var icons = new ProfileIconSet(
            Path.Combine(_iconsDirectory, iconBaseName + "-taskbar.ico"),
            Path.Combine(_iconsDirectory, iconBaseName + "-tray.ico"));

        if (File.Exists(icons.TaskbarIconFile) && File.Exists(icons.TrayIconFile))
        {
            return icons;
        }

        Directory.CreateDirectory(_iconsDirectory);
        DeleteIconsOf(profile);
        WriteIcon(icons.TaskbarIconFile, TaskbarIconSizes, size => RenderTaskbarImage(profile, logos, size));
        WriteIcon(icons.TrayIconFile, TrayIconSizes, size => RenderTrayImage(profile, logos, size));

        return icons;
    }

    private static string FolderNameOf(ClaudeProfile profile) => Path.GetFileName(profile.DataDirectory);

    private static string BuildIconBaseName(ClaudeProfile profile, bool usesClaudeLogo)
    {
        int initialCodePoint = char.ConvertToUtf32(profile.GetInitial(), 0);
        string artwork = usesClaudeLogo ? "logo" : "plain";
        return $"{FolderNameOf(profile)}{IconNameSeparator}{profile.Color}-{initialCodePoint:x}-{artwork}-v{ArtworkVersion}";
    }

    private void DeleteIconsOf(ClaudeProfile profile)
    {
        string profilePattern = FolderNameOf(profile) + IconNameSeparator + "*.ico";

        foreach (string outdatedIcon in Directory.EnumerateFiles(_iconsDirectory, profilePattern))
        {
            File.Delete(outdatedIcon);
        }
    }

    private static void WriteIcon(string iconFile, IReadOnlyList<int> sizes, Func<int, RenderTargetBitmap> render)
    {
        var images = new List<IcoImage>(sizes.Count);

        foreach (int size in sizes)
        {
            using RenderTargetBitmap image = render(size);
            images.Add(new IcoImage(size, EncodePng(image)));
        }

        using FileStream stream = File.Create(iconFile);
        IcoFile.Write(stream, images);
    }

    private static byte[] EncodePng(Bitmap image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, new PngBitmapEncoderOptions());
        return stream.ToArray();
    }

    private static RenderTargetBitmap RenderTaskbarImage(ClaudeProfile profile, ClaudeLogoSource logos, int size)
    {
        var image = new RenderTargetBitmap(new PixelSize(size, size));

        using (DrawingContext context = image.CreateDrawingContext())
        {
            DrawTile(context, profile, logos, size);

            if (size >= SmallestSizeWithInitialBadge && logos.IsAvailable)
            {
                DrawInitialBadge(context, profile, size);
            }
        }

        return image;
    }

    private static RenderTargetBitmap RenderTrayImage(ClaudeProfile profile, ClaudeLogoSource logos, int size)
    {
        var image = new RenderTargetBitmap(new PixelSize(size, size));

        using (DrawingContext context = image.CreateDrawingContext())
        {
            DrawTile(context, profile, logos, size);
        }

        return image;
    }

    private static void DrawTile(DrawingContext context, ClaudeProfile profile, ClaudeLogoSource logos, int size)
    {
        var bounds = new Rect(0, 0, size, size);
        Color profileColor = ProfilePalette.ColorOf(profile.Color);

        if (logos.FindLogoFor(size) is string logoFile)
        {
            using WriteableBitmap tintedLogo = TintLogo(logoFile, profileColor);
            context.DrawImage(tintedLogo, bounds);
            return;
        }

        double cornerRadius = size * TileCornerRatio;
        context.DrawRectangle(new SolidColorBrush(profileColor), null, bounds, cornerRadius, cornerRadius);
        DrawCenteredText(context, profile.GetInitial(), bounds.Center, size * TileInitialSizeRatio, Ivory);
    }

    private static void DrawInitialBadge(DrawingContext context, ClaudeProfile profile, int size)
    {
        double radius = size * BadgeDiameterRatio / 2;
        double ringThickness = Math.Max(1, size / SizePerRingPixel);
        var center = new Point(size - radius, size - radius);

        context.DrawEllipse(new SolidColorBrush(Ivory), null, center, radius, radius);
        context.DrawEllipse(new SolidColorBrush(ProfilePalette.ColorOf(profile.Color)), null, center, radius - ringThickness, radius - ringThickness);
        DrawCenteredText(context, profile.GetInitial(), center, radius * BadgeInitialSizeRatio, Ivory);
    }

    private static void DrawCenteredText(DrawingContext context, string text, Point center, double fontSize, Color color)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            fontSize,
            new SolidColorBrush(color));

        var origin = new Point(center.X - formattedText.Width / 2, center.Y - formattedText.Height / 2);
        context.DrawText(formattedText, origin);
    }

    private static WriteableBitmap TintLogo(string logoFile, Color profileColor)
    {
        using FileStream stream = File.OpenRead(logoFile);
        WriteableBitmap logo = WriteableBitmap.Decode(stream);

        using (ILockedFramebuffer framebuffer = logo.Lock())
        {
            TintPixels(framebuffer, profileColor);
        }

        return logo;
    }

    private static void TintPixels(ILockedFramebuffer framebuffer, Color profileColor)
    {
        bool isRedFirst = framebuffer.Format == PixelFormats.Rgba8888;
        bool isPremultiplied = framebuffer.AlphaFormat == AlphaFormat.Premul;
        int rowLength = framebuffer.Size.Width * BytesPerPixel;
        var row = new byte[rowLength];

        for (int y = 0; y < framebuffer.Size.Height; y++)
        {
            IntPtr rowAddress = framebuffer.Address + y * framebuffer.RowBytes;
            Marshal.Copy(rowAddress, row, 0, rowLength);

            for (int offset = 0; offset < rowLength; offset += BytesPerPixel)
            {
                TintPixel(row.AsSpan(offset, BytesPerPixel), profileColor, isRedFirst, isPremultiplied);
            }

            Marshal.Copy(row, 0, rowAddress, rowLength);
        }
    }

    // Le logo de Claude est un astérisque ivoire sur un fond terre cuite : le canal bleu, faible dans le fond et
    // élevé dans l'astérisque, sert de masque pour repeindre le fond dans la couleur du profil sans toucher au dessin.
    private static void TintPixel(Span<byte> pixel, Color profileColor, bool isRedFirst, bool isPremultiplied)
    {
        byte alpha = pixel[AlphaOffset];
        if (alpha == 0)
        {
            return;
        }

        int redOffset = isRedFirst ? 0 : 2;
        int blueOffset = isRedFirst ? 2 : 0;
        double premultiplication = isPremultiplied ? alpha / 255.0 : 1.0;
        double blueLevel = pixel[blueOffset] / premultiplication;
        double ivoryShare = Math.Clamp((blueLevel - LogoBackgroundBlueLevel) / (LogoForegroundBlueLevel - LogoBackgroundBlueLevel), 0, 1);

        pixel[redOffset] = MixChannel(profileColor.R, Ivory.R, ivoryShare, premultiplication);
        pixel[GreenOffset] = MixChannel(profileColor.G, Ivory.G, ivoryShare, premultiplication);
        pixel[blueOffset] = MixChannel(profileColor.B, Ivory.B, ivoryShare, premultiplication);
    }

    private static byte MixChannel(byte profileChannel, byte ivoryChannel, double ivoryShare, double premultiplication)
    {
        double mixed = profileChannel + (ivoryChannel - profileChannel) * ivoryShare;
        return (byte)Math.Round(mixed * premultiplication);
    }
}
