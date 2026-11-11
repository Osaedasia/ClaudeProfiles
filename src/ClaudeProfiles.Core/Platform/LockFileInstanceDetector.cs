namespace ClaudeProfiles.Core.Platform;

public sealed class LockFileInstanceDetector : IInstanceDetector
{
    private const string LockFileName = "lockfile";
    private const int SharingViolationHResult = unchecked((int)0x80070020);

    public bool IsRunning(string dataDirectory)
    {
        string lockFilePath = Path.Combine(dataDirectory, LockFileName);

        if (!File.Exists(lockFilePath))
        {
            return false;
        }

        // Chromium, et donc Electron, garde ce fichier ouvert en écriture pendant toute la vie de l'instance :
        // l'ouvrir sans partage échoue tant que Claude tourne, et réussit sur un fichier resté après un arrêt
        // brutal (sa simple présence ne prouve donc rien).
        // https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/process_singleton_win.cc
        try
        {
            using var probe = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (IOException ex) when (ex.HResult == SharingViolationHResult)
        {
            return true;
        }
    }
}
