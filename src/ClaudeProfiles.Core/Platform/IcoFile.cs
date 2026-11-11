namespace ClaudeProfiles.Core.Platform;

public sealed record IcoImage(int Size, byte[] Png);

public static class IcoFile
{
    private const ushort IconResourceType = 1;
    private const int HeaderLength = 6;
    private const int DirectoryEntryLength = 16;
    private const int LargestSizeStoredAsByte = 255;
    private const ushort ColorPlanes = 1;
    private const ushort BitsPerPixel = 32;

    public static void Write(Stream destination, IReadOnlyList<IcoImage> images)
    {
        using var writer = new BinaryWriter(destination, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0);
        writer.Write(IconResourceType);
        writer.Write((ushort)images.Count);

        int imageOffset = HeaderLength + DirectoryEntryLength * images.Count;

        foreach (IcoImage image in images)
        {
            WriteDirectoryEntry(writer, image, imageOffset);
            imageOffset += image.Png.Length;
        }

        foreach (IcoImage image in images)
        {
            writer.Write(image.Png);
        }
    }

    private static void WriteDirectoryEntry(BinaryWriter writer, IcoImage image, int imageOffset)
    {
        // Le format ICO code la dimension sur un octet : 0 y signifie 256 pixels.
        // https://learn.microsoft.com/en-us/previous-versions/ms997538(v=msdn.10)
        byte storedSize = image.Size > LargestSizeStoredAsByte ? (byte)0 : (byte)image.Size;

        writer.Write(storedSize);
        writer.Write(storedSize);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(ColorPlanes);
        writer.Write(BitsPerPixel);
        writer.Write(image.Png.Length);
        writer.Write(imageOffset);
    }
}
