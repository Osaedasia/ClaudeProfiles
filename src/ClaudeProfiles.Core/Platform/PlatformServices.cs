using ClaudeProfiles.Core.Platform.Windows;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform;

public static class PlatformServices
{
    public static ClaudeDataLocations CreateDataLocations()
    {
        if (OperatingSystem.IsWindows())
        {
            return new ClaudeDataLocations(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsMacOS())
        {
            return new ClaudeDataLocations(Path.Combine(userHome, "Library", "Application Support"), localDataRoot: null);
        }

        string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        string configRoot = string.IsNullOrEmpty(xdgConfigHome) ? Path.Combine(userHome, ".config") : xdgConfigHome;
        return new ClaudeDataLocations(configRoot, localDataRoot: null);
    }

    public static IInstanceDetector CreateInstanceDetector() =>
        OperatingSystem.IsWindows() ? new LockFileInstanceDetector() : new SingletonLockInstanceDetector();

    public static IInstanceMarker CreateInstanceMarker(IProfileIconFactory iconFactory, string managerExecutable) =>
        OperatingSystem.IsWindows() ? new WindowsInstanceMarker(iconFactory, managerExecutable) : new NoInstanceMarker();

    public static IClaudeLauncher CreateLauncher()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsClaudeLauncher(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacClaudeLauncher(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        return new LinuxClaudeLauncher();
    }
}
