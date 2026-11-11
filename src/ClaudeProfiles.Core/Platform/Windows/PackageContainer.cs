using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClaudeProfiles.Core.Platform.Windows;

[SupportedOSPlatform("windows")]
public static class PackageContainer
{
    private const int NoPackageIdentity = 15700;

    // Un processus lancé par une application MSIX, par exemple depuis Claude Code dans Claude Desktop, hérite de
    // son paquet : ses écritures dans AppData sont alors redirigées vers le stockage privé de Claude et deviennent
    // invisibles pour l'Explorateur et pour les autres applications (icônes vides, profils introuvables).
    // https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes
    public static bool IsCurrentProcessPackaged()
    {
        int nameLength = 0;
        return GetCurrentPackageFullName(ref nameLength, IntPtr.Zero) != NoPackageIdentity;
    }

    public static void RelaunchThroughExplorer(string executable)
    {
        var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
        startInfo.ArgumentList.Add(executable);

        using Process? explorer = Process.Start(startInfo);
    }

    [DllImport("kernel32.dll")]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);
}
