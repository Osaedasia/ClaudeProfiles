using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using ClaudeProfiles.Core.Platform;
using ClaudeProfiles.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClaudeProfiles.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ProfileCatalog _catalog;
    private readonly IClaudeLauncher _claudeLauncher;
    private readonly IInstanceDetector _instanceDetector;
    private readonly ILauncher _systemLauncher;
    private readonly IInstanceMarker _instanceMarker;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ProfileEditorViewModel? _editor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeletionCommand))]
    private ProfileItemViewModel? _pendingDeletion;

    public MainWindowViewModel(
        ProfileCatalog catalog,
        IClaudeLauncher claudeLauncher,
        IInstanceDetector instanceDetector,
        ILauncher systemLauncher,
        IInstanceMarker instanceMarker)
    {
        _catalog = catalog;
        _claudeLauncher = claudeLauncher;
        _instanceDetector = instanceDetector;
        _systemLauncher = systemLauncher;
        _instanceMarker = instanceMarker;
    }

    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            ShowProfiles(await _catalog.LoadAsync(cancellationToken));
        }
        catch (Exception exception) when (UserMessages.Describe(exception) is string message)
        {
            ErrorMessage = message;
        }
    }

    public void RefreshRunningInstances()
    {
        foreach (ProfileItemViewModel item in Profiles)
        {
            item.IsRunning = _instanceDetector.IsRunning(item.Profile.DataDirectory);

            if (item.IsRunning && !item.Profile.IsDefault)
            {
                MarkInstance(item);
            }
        }
    }

    public void OpenProfile(string dataDirectory)
    {
        ProfileItemViewModel? item = Profiles.FirstOrDefault(candidate =>
            ClaudeDataLocations.AreSameDirectory(candidate.Profile.DataDirectory, dataDirectory));

        if (item is null)
        {
            ErrorMessage = UserMessages.UnknownProfile(dataDirectory);
            return;
        }

        Launch(item);
    }

    public void Launch(ProfileItemViewModel item)
    {
        ErrorMessage = null;

        try
        {
            _claudeLauncher.Launch(item.Profile);
        }
        catch (Exception exception) when (UserMessages.Describe(exception) is string message)
        {
            ErrorMessage = message;
        }
    }

    public async Task OpenFolderAsync(ProfileItemViewModel item)
    {
        var dataDirectory = new DirectoryInfo(item.Profile.DataDirectory);
        bool isOpened = dataDirectory.Exists && await _systemLauncher.LaunchDirectoryInfoAsync(dataDirectory);

        if (!isOpened)
        {
            ErrorMessage = UserMessages.FolderCannotBeOpened(item.Profile.DataDirectory);
        }
    }

    public void StartEditing(ProfileItemViewModel item)
    {
        Editor = new ProfileEditorViewModel(
            EditorPurpose.Edit,
            item.Profile.DisplayName,
            item.Profile.Color,
            NamesOfProfilesOtherThan(item),
            (editor, cancellationToken) => ApplyChangeAsync(
                token => _catalog.UpdateAsync(item.Profile, editor.Name, editor.Color, token),
                cancellationToken));
    }

    public void RequestDeletion(ProfileItemViewModel item) => PendingDeletion = item;

    [RelayCommand]
    private void StartCreation()
    {
        List<ClaudeProfile> currentProfiles = Profiles
            .Select(item => item.Profile)
            .ToList();

        Editor = new ProfileEditorViewModel(
            EditorPurpose.Create,
            name: string.Empty,
            ProfileCatalog.PickUnusedColor(currentProfiles),
            NamesOfProfilesOtherThan(excludedItem: null),
            (editor, cancellationToken) => ApplyChangeAsync(
                token => _catalog.CreateAsync(editor.Name, editor.Color, token),
                cancellationToken));
    }

    [RelayCommand]
    private void CloseEditor() => Editor = null;

    [RelayCommand]
    private void CancelDeletion() => PendingDeletion = null;

    [RelayCommand(CanExecute = nameof(CanConfirmDeletion))]
    private async Task ConfirmDeletionAsync(CancellationToken cancellationToken)
    {
        if (PendingDeletion is not ProfileItemViewModel item)
        {
            return;
        }

        PendingDeletion = null;
        await ApplyChangeAsync(token => _catalog.DeleteAsync(item.Profile, token), cancellationToken);
    }

    private bool CanConfirmDeletion() => PendingDeletion is not null;

    [RelayCommand]
    private void DismissError() => ErrorMessage = null;

    private async Task ApplyChangeAsync(Func<CancellationToken, Task> change, CancellationToken cancellationToken)
    {
        Editor = null;
        ErrorMessage = null;

        try
        {
            await change(cancellationToken);
        }
        catch (Exception exception) when (UserMessages.Describe(exception) is string message)
        {
            ErrorMessage = message;
        }

        await LoadAsync(cancellationToken);
    }

    private void MarkInstance(ProfileItemViewModel item)
    {
        try
        {
            _instanceMarker.Mark(item.Profile);
        }
        catch (IOException exception)
        {
            ErrorMessage = UserMessages.InstanceCannotBeMarked(item.Profile.DisplayName, exception.Message);
        }
    }

    private void ShowProfiles(IReadOnlyList<ClaudeProfile> profiles)
    {
        Profiles.Clear();

        foreach (ClaudeProfile profile in profiles)
        {
            bool isRunning = _instanceDetector.IsRunning(profile.DataDirectory);
            Profiles.Add(new ProfileItemViewModel(profile, isRunning, this));
        }
    }

    private List<string> NamesOfProfilesOtherThan(ProfileItemViewModel? excludedItem) => Profiles
        .Where(item => item != excludedItem)
        .Select(item => item.Profile.DisplayName)
        .ToList();
}
