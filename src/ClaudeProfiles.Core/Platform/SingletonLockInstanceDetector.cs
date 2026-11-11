using System.Diagnostics;

namespace ClaudeProfiles.Core.Platform;

public sealed class SingletonLockInstanceDetector : IInstanceDetector
{
    private const string LockLinkName = "SingletonLock";

    public bool IsRunning(string dataDirectory)
    {
        var lockLink = new FileInfo(Path.Combine(dataDirectory, LockLinkName));

        // Chromium pose ici un lien symbolique « <hôte>-<pid> » qui ne pointe vers aucun fichier réel.
        // Il survit à un arrêt brutal : seul le processus qu'il désigne prouve que l'instance tourne encore.
        // https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/process_singleton_posix.cc
        if (lockLink.LinkTarget is not string lockTarget)
        {
            return false;
        }

        return TryReadProcessId(lockTarget, out int processId) && IsProcessAlive(processId);
    }

    private static bool TryReadProcessId(string lockTarget, out int processId)
    {
        int separatorIndex = lockTarget.LastIndexOf('-');
        return int.TryParse(lockTarget.AsSpan(separatorIndex + 1), out processId);
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
