using System.Collections.Frozen;

namespace ClaudeProfiles.Core.Profiles;

public sealed class ClaudeDataLocations
{
    private const string DefaultFolderName = "Claude";
    private const string ProfileFolderPrefix = "Claude-";
    private const string ManagerFolderName = "ClaudeProfiles";
    private const string ManagerSettingsFileName = "profiles.json";
    private const string ManagerIconsFolderName = "icons";
    private const string ChromiumProfileMarkerFile = "Local State";

    // Claude Desktop crée lui-même ces dossiers pour ses propres besoins (mode fournisseur tiers,
    // relocalisation des données) : ce ne sont pas des profils, et ils ne doivent jamais être supprimés d'ici.
    // Constaté dans les sources de l'application (app.asar, version 4.23118).
    private static readonly FrozenSet<string> FoldersReservedByClaude =
        new[] { "Claude-3p", "Claude-Data" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly string _userDataRoot;
    private readonly string? _localDataRoot;

    public ClaudeDataLocations(string userDataRoot, string? localDataRoot)
    {
        _userDataRoot = NormalizePath(userDataRoot);
        _localDataRoot = localDataRoot is null ? null : NormalizePath(localDataRoot);
    }

    public static StringComparer PathComparer { get; } =
        OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    public string DefaultDataDirectory => Path.Combine(_userDataRoot, DefaultFolderName);

    public string ManagerSettingsFile => Path.Combine(_userDataRoot, ManagerFolderName, ManagerSettingsFileName);

    public string ManagerIconsDirectory => Path.Combine(_userDataRoot, ManagerFolderName, ManagerIconsFolderName);

    public static string NormalizePath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    public static bool AreSameDirectory(string first, string second) =>
        PathComparer.Equals(NormalizePath(first), NormalizePath(second));

    public static string DisplayNameFromDirectory(string directory)
    {
        string folderName = Path.GetFileName(NormalizePath(directory));
        string nameWithoutPrefix = folderName.StartsWith(ProfileFolderPrefix, StringComparison.OrdinalIgnoreCase)
            ? folderName[ProfileFolderPrefix.Length..]
            : folderName;
        string displayName = nameWithoutPrefix.Replace('-', ' ').Trim();

        return displayName.Length == 0 ? folderName : displayName;
    }

    public string FindFreeDirectoryForNewProfile(string folderSlug)
    {
        string baseName = ProfileFolderPrefix + folderSlug;
        string candidate = Path.Combine(_userDataRoot, baseName);

        for (int suffix = 2; Directory.Exists(candidate) || IsReservedByClaude(candidate); suffix++)
        {
            candidate = Path.Combine(_userDataRoot, $"{baseName}-{suffix}");
        }

        return candidate;
    }

    public IEnumerable<string> FindExistingProfileDirectories()
    {
        if (!Directory.Exists(_userDataRoot))
        {
            return [];
        }

        return Directory.EnumerateDirectories(_userDataRoot, ProfileFolderPrefix + "*")
            .Where(directory => !IsReservedByClaude(directory))
            .Where(directory => File.Exists(Path.Combine(directory, ChromiumProfileMarkerFile)));
    }

    public IReadOnlyList<string> GetDirectoriesToDelete(ClaudeProfile profile)
    {
        if (profile.IsDefault || !IsDeletableProfileDirectory(profile.DataDirectory))
        {
            throw new InvalidOperationException(
                $"Refusing to delete '{profile.DataDirectory}': it is not a profile folder managed by this application.");
        }

        return GetProfileDirectories(profile.DataDirectory);
    }

    public IReadOnlyList<string> GetProfileDirectories(string dataDirectory)
    {
        if (_localDataRoot is null)
        {
            return [dataDirectory];
        }

        // Sous Windows, Claude écrit aussi ses journaux dans un dossier homonyme de %LOCALAPPDATA%
        // (constaté avec la version 4.23118 : %LOCALAPPDATA%\Claude-Travail\logs\main.log).
        string folderName = Path.GetFileName(NormalizePath(dataDirectory));
        return [dataDirectory, Path.Combine(_localDataRoot, folderName)];
    }

    private bool IsDeletableProfileDirectory(string directory)
    {
        string normalizedDirectory = NormalizePath(directory);
        string? parent = Path.GetDirectoryName(normalizedDirectory);

        return parent is not null
            && PathComparer.Equals(parent, _userDataRoot)
            && Path.GetFileName(normalizedDirectory).StartsWith(ProfileFolderPrefix, StringComparison.OrdinalIgnoreCase)
            && !IsReservedByClaude(normalizedDirectory);
    }

    private static bool IsReservedByClaude(string directory) =>
        FoldersReservedByClaude.Contains(Path.GetFileName(directory));
}
