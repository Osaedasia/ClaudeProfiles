using System.Diagnostics;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform;

internal static class ClaudeProcess
{
    // Option Chromium qu'Electron applique à son dossier userData. Claude la respecte, alors que ses versions
    // signées ignorent la variable d'environnement CLAUDE_USER_DATA_DIR (constaté dans app.asar, version 4.23118).
    private const string UserDataDirectorySwitch = "--user-data-dir=";

    public static IEnumerable<string> DataDirectoryArguments(ClaudeProfile profile) =>
        profile.IsDefault ? [] : [UserDataDirectorySwitch + profile.DataDirectory];

    public static void Start(string fileName, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo(fileName) { UseShellExecute = false };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process? process = Process.Start(startInfo);
    }
}
