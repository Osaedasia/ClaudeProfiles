using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform;

public sealed class WindowsClaudeLauncher : IClaudeLauncher
{
    private readonly IReadOnlyList<string> _executableCandidates;

    public WindowsClaudeLauncher(string localApplicationData)
    {
        string msixExecutionAlias = Path.Combine(localApplicationData, "Microsoft", "WindowsApps", "claude-desktop.exe");
        string legacyInstallerExecutable = Path.Combine(localApplicationData, "AnthropicClaude", "claude.exe");
        _executableCandidates = [msixExecutionAlias, legacyInstallerExecutable];
    }

    public void Launch(ClaudeProfile profile)
    {
        string executable = _executableCandidates.FirstOrDefault(File.Exists)
            ?? throw new ClaudeNotInstalledException(_executableCandidates);

        ClaudeProcess.Start(executable, ClaudeProcess.DataDirectoryArguments(profile));
    }
}

public sealed class MacClaudeLauncher : IClaudeLauncher
{
    private const string OpenCommand = "open";

    private readonly IReadOnlyList<string> _bundleCandidates;

    public MacClaudeLauncher(string userHome)
    {
        _bundleCandidates = ["/Applications/Claude.app", Path.Combine(userHome, "Applications", "Claude.app")];
    }

    public void Launch(ClaudeProfile profile)
    {
        string bundle = _bundleCandidates.FirstOrDefault(Directory.Exists)
            ?? throw new ClaudeNotInstalledException(_bundleCandidates);

        ClaudeProcess.Start(OpenCommand, BuildOpenArguments(bundle, profile));
    }

    private static List<string> BuildOpenArguments(string bundle, ClaudeProfile profile)
    {
        if (profile.IsDefault)
        {
            return ["-a", bundle];
        }

        // Sans -n, macOS ramène au premier plan l'instance déjà ouverte, quel que soit son profil,
        // au lieu d'en démarrer une sur le dossier demandé.
        return ["-n", "-a", bundle, "--args", .. ClaudeProcess.DataDirectoryArguments(profile)];
    }
}

public sealed class LinuxClaudeLauncher : IClaudeLauncher
{
    private const string ExecutableName = "claude-desktop";

    public void Launch(ClaudeProfile profile)
    {
        string executable = FindExecutableOnPath()
            ?? throw new ClaudeNotInstalledException([ExecutableName]);

        ClaudeProcess.Start(executable, ClaudeProcess.DataDirectoryArguments(profile));
    }

    private static string? FindExecutableOnPath()
    {
        string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        return pathVariable
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, ExecutableName))
            .FirstOrDefault(File.Exists);
    }
}
