using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Tests;

public sealed class ProfileCatalogTests : IDisposable
{
    private readonly TemporaryDirectory _userDataRoot = new();
    private readonly TemporaryDirectory _localDataRoot = new();
    private readonly FakeInstanceDetector _instanceDetector = new();
    private readonly ProfileCatalog _catalog;

    public ProfileCatalogTests()
    {
        var locations = new ClaudeDataLocations(_userDataRoot.Path, _localDataRoot.Path);
        _catalog = new ProfileCatalog(new JsonProfileStore(locations.ManagerSettingsFile), locations, _instanceDetector);
    }

    [Fact]
    public async Task Load_WithNothingStored_ReturnsOnlyTheDefaultProfile()
    {
        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        ClaudeProfile defaultProfile = Assert.Single(profiles);
        Assert.True(defaultProfile.IsDefault);
        Assert.Equal(ProfileCatalog.DefaultProfileName, defaultProfile.DisplayName);
        Assert.Equal(Path.Combine(_userDataRoot.Path, "Claude"), defaultProfile.DataDirectory);
    }

    [Fact]
    public async Task Load_DiscoversProfileFoldersCreatedOutsideTheApplication()
    {
        string workDirectory = _userDataRoot.CreateSubdirectory("Claude-Client-Dupont", withChromiumProfile: true);

        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        ClaudeProfile discovered = Assert.Single(profiles, profile => !profile.IsDefault);
        Assert.Equal("Client Dupont", discovered.DisplayName);
        Assert.Equal(workDirectory, discovered.DataDirectory);
    }

    [Fact]
    public async Task Load_IgnoresFoldersThatAreNotProfiles()
    {
        _userDataRoot.CreateSubdirectory("Claude-3p", withChromiumProfile: true);
        _userDataRoot.CreateSubdirectory("Claude-Empty", withChromiumProfile: false);
        _userDataRoot.CreateSubdirectory("Other-App", withChromiumProfile: true);

        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        Assert.Single(profiles);
    }

    [Fact]
    public async Task Load_ForgetsProfilesWhoseFolderWasRemoved()
    {
        ClaudeProfile created = await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);
        Directory.Delete(created.DataDirectory);

        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        Assert.DoesNotContain(profiles, profile => profile.DisplayName == "Travail");
    }

    [Fact]
    public async Task Create_MakesTheDataFolderAndKeepsTheProfile()
    {
        ClaudeProfile created = await _catalog.CreateAsync("  Équipe été ", ProfileColor.Olive, CancellationToken.None);

        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        Assert.Equal(Path.Combine(_userDataRoot.Path, "Claude-Equipe-ete"), created.DataDirectory);
        Assert.True(Directory.Exists(created.DataDirectory));
        Assert.True(Directory.Exists(Path.Combine(_localDataRoot.Path, "Claude-Equipe-ete")));
        Assert.Contains(new ClaudeProfile("Équipe été", created.DataDirectory, ProfileColor.Olive, IsDefault: false), profiles);
    }

    [Fact]
    public async Task Create_WhenTheFolderNameIsTaken_PicksAFreeOne()
    {
        _userDataRoot.CreateSubdirectory("Claude-Travail", withChromiumProfile: false);

        ClaudeProfile created = await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);

        Assert.Equal(Path.Combine(_userDataRoot.Path, "Claude-Travail-2"), created.DataDirectory);
    }

    [Fact]
    public async Task Create_WithANameAlreadyUsed_IsRejected()
    {
        await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);

        var rejection = await Assert.ThrowsAsync<ProfileNameRejectedException>(
            () => _catalog.CreateAsync(" travail ", ProfileColor.Plum, CancellationToken.None));

        Assert.Equal(ProfileNameIssue.AlreadyUsed, rejection.Issue);
    }

    [Fact]
    public async Task Update_ChangesNameAndColorButKeepsTheFolder()
    {
        ClaudeProfile created = await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);

        await _catalog.UpdateAsync(created, "Bureau", ProfileColor.Plum, CancellationToken.None);
        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        Assert.Contains(new ClaudeProfile("Bureau", created.DataDirectory, ProfileColor.Plum, IsDefault: false), profiles);
        Assert.DoesNotContain(profiles, profile => profile.DisplayName == "Travail");
    }

    [Fact]
    public async Task Update_KeepingItsOwnName_IsAccepted()
    {
        ClaudeProfile created = await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);

        await _catalog.UpdateAsync(created, "Travail", ProfileColor.Sand, CancellationToken.None);
        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        Assert.Contains(profiles, profile => profile.DisplayName == "Travail" && profile.Color == ProfileColor.Sand);
    }

    [Fact]
    public async Task Delete_RemovesTheProfileAndAllItsFolders()
    {
        ClaudeProfile created = await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);
        string logsDirectory = _localDataRoot.CreateSubdirectory("Claude-Travail", withChromiumProfile: false);

        await _catalog.DeleteAsync(created, CancellationToken.None);
        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        Assert.False(Directory.Exists(created.DataDirectory));
        Assert.False(Directory.Exists(logsDirectory));
        Assert.Single(profiles);
    }

    [Fact]
    public async Task Delete_WhileClaudeRunsOnTheProfile_IsRefused()
    {
        ClaudeProfile created = await _catalog.CreateAsync("Travail", ProfileColor.Sky, CancellationToken.None);
        _instanceDetector.RunningDirectories.Add(created.DataDirectory);

        await Assert.ThrowsAsync<ProfileInUseException>(() => _catalog.DeleteAsync(created, CancellationToken.None));

        Assert.True(Directory.Exists(created.DataDirectory));
    }

    [Fact]
    public async Task Delete_TheDefaultProfile_IsRefused()
    {
        IReadOnlyList<ClaudeProfile> profiles = await _catalog.LoadAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _catalog.DeleteAsync(profiles[0], CancellationToken.None));
    }

    [Fact]
    public async Task Delete_AFolderOutsideTheClaudeDataRoot_IsRefused()
    {
        using var unrelatedRoot = new TemporaryDirectory();
        string unrelatedDirectory = unrelatedRoot.CreateSubdirectory("Claude-Unrelated", withChromiumProfile: true);
        var unrelatedProfile = new ClaudeProfile("Unrelated", unrelatedDirectory, ProfileColor.Slate, IsDefault: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _catalog.DeleteAsync(unrelatedProfile, CancellationToken.None));

        Assert.True(Directory.Exists(unrelatedDirectory));
    }

    [Fact]
    public void PickUnusedColor_PrefersAColorNoProfileUsesYet()
    {
        ClaudeProfile[] profiles =
        [
            new("Perso", "a", ProfileColor.Clay, IsDefault: true),
            new("Travail", "b", ProfileColor.Sky, IsDefault: false)
        ];

        Assert.Equal(ProfileColor.Olive, ProfileCatalog.PickUnusedColor(profiles));
    }

    public void Dispose()
    {
        _userDataRoot.Dispose();
        _localDataRoot.Dispose();
    }
}
