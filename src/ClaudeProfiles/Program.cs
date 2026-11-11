using Avalonia;
using ClaudeProfiles.Core.Instance;
using ClaudeProfiles.Core.Platform.Windows;

namespace ClaudeProfiles;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows() && PackageContainer.IsCurrentProcessPackaged() && Environment.ProcessPath is string executable)
        {
            PackageContainer.RelaunchThroughExplorer(executable);
            return;
        }

        ManagerMessage startupMessage = ManagerMessage.FromCommandLine(args);

        if (ManagerInstanceChannel.TrySendToRunningInstance(startupMessage))
        {
            return;
        }

        BuildAvaloniaApp(startupMessage).StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(ManagerMessage.ShowWindow);

    private static AppBuilder BuildAvaloniaApp(ManagerMessage startupMessage) =>
        AppBuilder.Configure(() => new App(startupMessage))
            .UsePlatformDetect()
            .LogToTrace();
}
