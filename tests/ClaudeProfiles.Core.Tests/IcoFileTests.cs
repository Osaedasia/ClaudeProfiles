using ClaudeProfiles.Core.Platform;

namespace ClaudeProfiles.Core.Tests;

public sealed class IcoFileTests
{
    [Fact]
    public void Write_ProducesAHeaderAndOneEntryPerImage()
    {
        IcoImage[] images = [new(16, [1, 2, 3]), new(256, [4, 5])];
        using var stream = new MemoryStream();

        IcoFile.Write(stream, images);

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());
        Assert.Equal(2, reader.ReadUInt16());
        AssertEntry(reader, expectedStoredSize: 16, expectedLength: 3, expectedOffset: 38);
        AssertEntry(reader, expectedStoredSize: 0, expectedLength: 2, expectedOffset: 41);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reader.ReadBytes(5));
    }

    private static void AssertEntry(BinaryReader reader, byte expectedStoredSize, int expectedLength, int expectedOffset)
    {
        Assert.Equal(expectedStoredSize, reader.ReadByte());
        Assert.Equal(expectedStoredSize, reader.ReadByte());
        reader.ReadBytes(6);
        Assert.Equal(expectedLength, reader.ReadInt32());
        Assert.Equal(expectedOffset, reader.ReadInt32());
    }
}
