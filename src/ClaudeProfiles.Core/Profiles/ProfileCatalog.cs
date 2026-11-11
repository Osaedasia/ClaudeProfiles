using ClaudeProfiles.Core.Platform;

namespace ClaudeProfiles.Core.Profiles;

public sealed class ProfileCatalog
{
    public const string DefaultProfileName = "Perso";

    private readonly JsonProfileStore _store;
    private readonly ClaudeDataLocations _locations;
    private readonly IInstanceDetector _instanceDetector;

    public ProfileCatalog(JsonProfileStore store, ClaudeDataLocations locations, IInstanceDetector instanceDetector)
    {
        _store = store;
        _locations = locations;
        _instanceDetector = instanceDetector;
    }

    public static ProfileColor PickUnusedColor(IReadOnlyCollection<ClaudeProfile> profiles)
    {
        HashSet<ProfileColor> usedColors = profiles
            .Select(profile => profile.Color)
            .ToHashSet();
        ProfileColor[] allColors = Enum.GetValues<ProfileColor>();
        ProfileColor colorWhenAllAreUsed = allColors[profiles.Count % allColors.Length];

        return allColors.FirstOrDefault(color => !usedColors.Contains(color), colorWhenAllAreUsed);
    }

    public async Task<IReadOnlyList<ClaudeProfile>> LoadAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ClaudeProfile> storedProfiles = await _store.LoadAsync(cancellationToken);
        List<ClaudeProfile> profiles = Reconcile(storedProfiles);

        if (!profiles.SequenceEqual(storedProfiles))
        {
            await _store.SaveAsync(profiles, cancellationToken);
        }

        return profiles
            .OrderByDescending(profile => profile.IsDefault)
            .ThenBy(profile => profile.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<ClaudeProfile> CreateAsync(string displayName, ProfileColor color, CancellationToken cancellationToken)
    {
        IReadOnlyList<ClaudeProfile> profiles = await LoadAsync(cancellationToken);
        string acceptedName = EnsureNameAccepted(displayName, profiles);
        string dataDirectory = _locations.FindFreeDirectoryForNewProfile(ProfileFolderName.ToSlug(acceptedName));

        // Les dossiers sont créés ici plutôt que laissés à Claude : installé en MSIX, Claude voit tout nouveau
        // dossier qu'il crée dans AppData redirigé vers son stockage privé (%LOCALAPPDATA%\Packages\Claude_…\
        // LocalCache), invisible des autres applications ; un dossier qui existe déjà est en revanche écrit sur
        // place, comme son dossier « Claude » d'origine (constaté avec la version 4.23118).
        // https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes
        foreach (string directory in _locations.GetProfileDirectories(dataDirectory))
        {
            Directory.CreateDirectory(directory);
        }

        var createdProfile = new ClaudeProfile(acceptedName, dataDirectory, color, IsDefault: false);
        await _store.SaveAsync([.. profiles, createdProfile], cancellationToken);

        return createdProfile;
    }

    public async Task UpdateAsync(ClaudeProfile profile, string displayName, ProfileColor color, CancellationToken cancellationToken)
    {
        IReadOnlyList<ClaudeProfile> profiles = await LoadAsync(cancellationToken);
        IEnumerable<ClaudeProfile> otherProfiles = profiles.Where(other => !IsSameProfile(other, profile));
        string acceptedName = EnsureNameAccepted(displayName, otherProfiles);
        ClaudeProfile updatedProfile = profile with { DisplayName = acceptedName, Color = color };

        List<ClaudeProfile> updatedProfiles = profiles
            .Select(existing => IsSameProfile(existing, profile) ? updatedProfile : existing)
            .ToList();

        await _store.SaveAsync(updatedProfiles, cancellationToken);
    }

    public async Task DeleteAsync(ClaudeProfile profile, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> directoriesToDelete = _locations.GetDirectoriesToDelete(profile);

        if (_instanceDetector.IsRunning(profile.DataDirectory))
        {
            throw new ProfileInUseException(profile);
        }

        await Task.Run(() => DeleteDirectories(directoriesToDelete), cancellationToken);
        await LoadAsync(cancellationToken);
    }

    private static void DeleteDirectories(IReadOnlyList<string> directories)
    {
        foreach (string directory in directories.Where(Directory.Exists))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private List<ClaudeProfile> Reconcile(IReadOnlyList<ClaudeProfile> storedProfiles)
    {
        List<ClaudeProfile> profiles = storedProfiles
            .Where(profile => profile.IsDefault || Directory.Exists(profile.DataDirectory))
            .ToList();

        if (!profiles.Any(profile => profile.IsDefault))
        {
            profiles.Insert(0, new ClaudeProfile(DefaultProfileName, _locations.DefaultDataDirectory, ProfileColor.Clay, IsDefault: true));
        }

        AddDiscoveredProfiles(profiles);
        return profiles;
    }

    private void AddDiscoveredProfiles(List<ClaudeProfile> profiles)
    {
        HashSet<string> knownDirectories = profiles
            .Select(profile => ClaudeDataLocations.NormalizePath(profile.DataDirectory))
            .ToHashSet(ClaudeDataLocations.PathComparer);

        List<string> unknownDirectories = _locations.FindExistingProfileDirectories()
            .Where(directory => !knownDirectories.Contains(ClaudeDataLocations.NormalizePath(directory)))
            .ToList();

        foreach (string directory in unknownDirectories)
        {
            string displayName = ClaudeDataLocations.DisplayNameFromDirectory(directory);
            profiles.Add(new ClaudeProfile(displayName, directory, PickUnusedColor(profiles), IsDefault: false));
        }
    }

    private static string EnsureNameAccepted(string displayName, IEnumerable<ClaudeProfile> otherProfiles)
    {
        IEnumerable<string> namesInUse = otherProfiles.Select(profile => profile.DisplayName);
        ProfileNameIssue issue = ProfileNameValidator.Validate(displayName, namesInUse);

        if (issue != ProfileNameIssue.None)
        {
            throw new ProfileNameRejectedException(displayName, issue);
        }

        return displayName.Trim();
    }

    private static bool IsSameProfile(ClaudeProfile first, ClaudeProfile second) =>
        ClaudeDataLocations.AreSameDirectory(first.DataDirectory, second.DataDirectory);
}
