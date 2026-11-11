using ClaudeProfiles.Core.Platform;

namespace ClaudeProfiles.Core.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ClaudeProfilesTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateSubdirectory(string name, bool withChromiumProfile)
    {
        string directory = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(directory);

        if (withChromiumProfile)
        {
            File.WriteAllText(System.IO.Path.Combine(directory, "Local State"), "{}");
        }

        return directory;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal sealed class FakeInstanceDetector : IInstanceDetector
{
    public HashSet<string> RunningDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning(string dataDirectory) => RunningDirectories.Contains(dataDirectory);
}

internal sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Windows only.";
        }
    }
}

internal sealed class PosixFactAttribute : FactAttribute
{
    public PosixFactAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "macOS and Linux only.";
        }
    }
}
