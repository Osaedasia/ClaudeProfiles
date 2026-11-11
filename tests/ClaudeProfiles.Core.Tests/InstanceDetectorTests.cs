using ClaudeProfiles.Core.Platform;

namespace ClaudeProfiles.Core.Tests;

public sealed class InstanceDetectorTests : IDisposable
{
    private readonly TemporaryDirectory _dataDirectory = new();

    [WindowsFact]
    public void LockFile_HeldOpenByAnotherHandle_MeansRunning()
    {
        string lockFilePath = Path.Combine(_dataDirectory.Path, "lockfile");
        using var heldLock = new FileStream(lockFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        Assert.True(new LockFileInstanceDetector().IsRunning(_dataDirectory.Path));
    }

    [WindowsFact]
    public void LockFile_LeftBehindAfterACrash_MeansNotRunning()
    {
        File.WriteAllText(Path.Combine(_dataDirectory.Path, "lockfile"), string.Empty);

        Assert.False(new LockFileInstanceDetector().IsRunning(_dataDirectory.Path));
    }

    [Fact]
    public void MissingDataDirectory_MeansNotRunning()
    {
        string missingDirectory = Path.Combine(_dataDirectory.Path, "missing");

        Assert.False(new LockFileInstanceDetector().IsRunning(missingDirectory));
        Assert.False(new SingletonLockInstanceDetector().IsRunning(missingDirectory));
    }

    [PosixFact]
    public void SingletonLock_PointingToALiveProcess_MeansRunning()
    {
        File.CreateSymbolicLink(Path.Combine(_dataDirectory.Path, "SingletonLock"), $"host-{Environment.ProcessId}");

        Assert.True(new SingletonLockInstanceDetector().IsRunning(_dataDirectory.Path));
    }

    public void Dispose() => _dataDirectory.Dispose();
}
